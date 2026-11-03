using System.Globalization;
using System.Text.RegularExpressions;
using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Dishes;
using Restaurant_Aqa_Framework.Api.Dtos.GroupOrders;
using Restaurant_Aqa_Framework.Api.Dtos.Orders;
using Restaurant_Aqa_Framework.Screenplay.Tasks;
using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Questions;

/// <summary>The credential a persona calls with; an anonymous visitor has none.</summary>
public sealed class TheTokenOf(string persona) : IQuestion<string?>
{
    public async Task<string?> AnsweredByAsync(Actor actor)
    {
        switch (persona)
        {
            case "the owner":
                return actor.Recall<CustomerSession>("Owner").IdToken;
            case "another customer":
                await actor.AttemptsToAsync(new RegisterACustomer("Another customer"));
                return actor.Recall<CustomerSession>("Another customer").IdToken;
            case "the waiter":
                return actor.Recall<CustomerSession>("Waiter").IdToken;
            case "a guest":
                return actor.RecallAll<GuestInGroup>()[0].Token;
            case "an anonymous visitor":
                return null;
            default:
                throw new InvalidOperationException($"Unknown persona: {persona}.");
        }
    }
}

/// <summary>The group as the service reports it to the Owner.</summary>
public sealed class TheGroupOrderReported : IQuestion<GroupOrderView>
{
    public Task<GroupOrderView> AnsweredByAsync(Actor actor)
    {
        var snapshot = actor.TheLastResponse().Json<GroupSnapshot>();
        var guests = snapshot.Participants
            .Where(p => p.Type == "Guest")
            .Select(p => new GuestLine(
                p.Name,
                GroupOrderView.Describe(p.Items.Select(i => (i.Dish.Name, i.Quantity))),
                p.Total,
                p.IsSubmitted ? "submitted" : "still choosing"))
            .ToList();

        return Task.FromResult(new GroupOrderView(guests, snapshot.Total));
    }
}

/// <summary>The group as the Owner's screen shows it.</summary>
public sealed partial class TheGroupOrderShown : IQuestion<GroupOrderView>
{
    public async Task<GroupOrderView> AnsweredByAsync(Actor actor)
    {
        var page = new GroupOrderPage(actor.LookingAtThePage());
        await page.GrandTotal.WaitForAsync();

        var guests = new List<GuestLine>();
        foreach (var card in await page.ParticipantCards.AllAsync())
        {
            var title = await card.Locator(".title").InnerTextAsync();
            if (!title.StartsWith("Guest: ", StringComparison.Ordinal))
                continue;

            var items = new List<(string, int)>();
            foreach (var item in await card.Locator(".item").AllAsync())
            {
                var quantity = await item.Locator(".qty").InnerTextAsync();
                items.Add((await item.Locator(".name").InnerTextAsync(), int.Parse(Number().Match(quantity).Value, CultureInfo.InvariantCulture)));
            }

            guests.Add(new GuestLine(
                title["Guest: ".Length..],
                GroupOrderView.Describe(items),
                Money(await card.Locator(".subtotal span").Last.InnerTextAsync()),
                (await card.Locator(".chip").InnerTextAsync()).Trim().ToLowerInvariant()));
        }

        return new GroupOrderView(guests, Money(await page.GrandTotal.InnerTextAsync()));
    }

    private static decimal Money(string text) =>
        decimal.Parse(text.Replace("$", "", StringComparison.Ordinal).Replace(",", "", StringComparison.Ordinal), CultureInfo.InvariantCulture);

    [GeneratedRegex(@"\d+")]
    private static partial Regex Number();
}

/// <summary>The consolidated dishes on the Owner's screen, as "Dish x quantity" lines.</summary>
public sealed partial class TheConsolidatedDishesShown : IQuestion<string>
{
    public async Task<string> AnsweredByAsync(Actor actor)
    {
        var lines = await new GroupOrderPage(actor.LookingAtThePage()).ConsolidatedItems.AllInnerTextsAsync();
        return GroupOrderView.Describe(lines.Select(line =>
        {
            var match = Line().Match(line);
            return (match.Groups[1].Value.Trim(), int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture));
        }));
    }

    [GeneratedRegex(@"^(.+?)\s*×\s*(\d+)")]
    private static partial Regex Line();
}

/// <summary>What the last consolidated submit said it sent, as "Dish x quantity" lines.</summary>
public sealed class TheDishesSubmitted : IQuestion<string>
{
    public Task<string> AnsweredByAsync(Actor actor)
    {
        var dishes = actor.RecallAll<DishResponse>();
        var submitted = actor.TheLastResponse().Json<List<SubmittedDish>>();
        return Task.FromResult(GroupOrderView.Describe(
            submitted.Select(s => (dishes.Single(d => d.Id == s.DishId).Name, s.Quantity))));
    }
}

/// <summary>The order the waiter gets from the service.</summary>
public sealed class TheOrderForTheWaiter : IQuestion<(OrderStatus Status, string Dishes)>
{
    public async Task<(OrderStatus Status, string Dishes)> AnsweredByAsync(Actor actor)
    {
        var waiter = actor.Recall<CustomerSession>("Waiter");
        var response = await new OrdersApi(actor.UsingTheApi()).GetOrderAsync(actor.Recall<GroupInPlay>().ReservationId, waiter.IdToken);
        var order = await SetUpSteps.ExpectAsync<GetOrderResponse>(response, "Reading the order as the waiter");
        return (order.Status, GroupOrderView.Describe(order.OrderItems.Select(i => (i.Dish.Name, i.Quantity))));
    }
}

/// <summary>The dishes on the waiter's pre-order screen, as "Dish x quantity" lines.</summary>
public sealed partial class ThePreOrderTheWaiterSees : IQuestion<string>
{
    public async Task<string> AnsweredByAsync(Actor actor)
    {
        var dishes = actor.RecallAll<DishResponse>();
        var items = new List<(string, int)>();
        foreach (var card in await new WaiterPreOrderPage(actor.LookingAtThePage()).DishCards.AllAsync())
        {
            var text = await card.InnerTextAsync();
            items.Add((
                dishes.Single(d => text.Contains(d.Name, StringComparison.Ordinal)).Name,
                int.Parse(Quantity().Match(text).Groups[1].Value, CultureInfo.InvariantCulture)));
        }

        return GroupOrderView.Describe(items);
    }

    [GeneratedRegex(@"Quantity:\s*(\d+)")]
    private static partial Regex Quantity();
}

/// <summary>The group's status as the service reports it.</summary>
public sealed class TheGroupStatus : IQuestion<string>
{
    public async Task<string> AnsweredByAsync(Actor actor)
    {
        var response = await new GroupOrdersApi(actor.UsingTheApi()).GetStatusAsync(actor.Recall<GroupInPlay>().GroupId);
        return (await SetUpSteps.ExpectAsync<GroupStatusResponse>(response, "Reading the group status")).Status;
    }
}
