using Microsoft.Playwright;
using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Auth;
using Restaurant_Aqa_Framework.Api.Dtos.Dishes;
using Restaurant_Aqa_Framework.Api.Dtos.GroupOrders;
using Restaurant_Aqa_Framework.Api.Dtos.Locations;
using Restaurant_Aqa_Framework.Api.Dtos.Orders;
using Restaurant_Aqa_Framework.Api.Dtos.Reservations;
using Restaurant_Aqa_Framework.Api.Dtos.Tables;
using Restaurant_Aqa_Framework.Api.Seeding;
using Restaurant_Aqa_Framework.Screenplay.Questions;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

internal static class SetUpSteps
{
    /// <summary>A set-up call that fails stops the scenario here, not in a confusing later step.</summary>
    public static async Task<T> ExpectAsync<T>(IAPIResponse response, string what)
    {
        if (!response.Ok)
            throw new InvalidOperationException($"{what} failed with {response.Status}: {await response.TextAsync()}");

        return await response.ReadAsAsync<T>();
    }

    public static async Task ExpectOkAsync(IAPIResponse response, string what)
    {
        if (!response.Ok)
            throw new InvalidOperationException($"{what} failed with {response.Status}: {await response.TextAsync()}");
    }
}

/// <summary>Registers and signs in a new customer, remembered under the role they play.</summary>
public sealed class RegisterACustomer(string role) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var api = new AuthApi(actor.UsingTheApi());
        var request = SampleDataFactory.RegisterRequest();

        await SetUpSteps.ExpectOkAsync(await api.RegisterAsync(request), "Registering the customer");
        var signIn = await SetUpSteps.ExpectAsync<SignInResponse>(
            await api.SignInAsync(new SignInRequest(request.Email, request.Password)), "Signing the customer in");

        actor.Remember(role, new CustomerSession(request.Email, request.Password, request.FirstName, signIn.IdToken));
    }
}

/// <summary>Opens a restaurant of the scenario's own: a location, a waiter who works there and a menu with known
/// prices. Nothing is shared with a parallel scenario, which matters because the backend saves a location as a whole
/// (read, change, write), so two dishes added to one location at the same moment can overwrite each other.</summary>
public sealed class OpenARestaurant(IReadOnlyList<(string Name, decimal Price)> dishes) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        await actor.AttemptsToAsync(new SetUpLocation());
        var location = actor.Recall<LocationResponse>();
        var standingWaiter = await actor.AsksForAsync(new TheWaiterSession());
        var scope = actor.Recall<CleanupScope>();

        await actor.AttemptsToAsync(new RegisterACustomer("Waiter"));
        var waiter = actor.Recall<CustomerSession>("Waiter");
        var groups = new GroupOrdersApi(actor.UsingTheApi());
        await SetUpSteps.ExpectOkAsync(await groups.AddWaiterAsync(waiter.Email, location.Id), "Hiring the waiter");
        scope.TrackDelete($"waiter:{waiter.Email}", () => groups.RemoveWaiterAsync(waiter.Email, location.Id));

        // The role is part of the token, so the token from before the waiter was hired will not do.
        var signIn = await SetUpSteps.ExpectAsync<SignInResponse>(
            await new AuthApi(actor.UsingTheApi()).SignInAsync(new SignInRequest(waiter.Email, waiter.Password)),
            "Signing the waiter in");
        actor.Remember("Waiter", waiter with { IdToken = signIn.IdToken });

        var api = new DishesApi(actor.UsingTheApi());
        foreach (var (name, price) in dishes)
        {
            var request = SampleDataFactory.CreateDishRequest([location.Id]) with
            {
                Name = MenuName.Unique(name),
                Price = price,
            };
            var dish = await SetUpSteps.ExpectAsync<DishResponse>(await api.CreateDishAsync(request), $"Adding {name}");

            actor.Remember(name, dish);
            scope.TrackDelete(
                $"dish:{dish.Id}", () => api.DeleteDishAsync(dish.Id, force: true, bearerToken: standingWaiter.IdToken));
        }
    }
}

/// <summary>The Owner books a table of their own and gets the group that comes with it. The table is unique to the
/// scenario, so scenarios cannot collide over a time slot; the date is always in the future.</summary>
public sealed class BookAGroupReservation(int guests, string? name = null) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var owner = actor.Recall<CustomerSession>("Owner");
        var location = actor.Recall<LocationResponse>();
        var waiter = await actor.AsksForAsync(new TheWaiterSession());
        var scope = actor.Recall<CleanupScope>();

        var tables = new TablesApi(actor.UsingTheApi());
        var table = await SetUpSteps.ExpectAsync<TableResponse>(
            await tables.CreateTableAsync(SampleDataFactory.CreateTableRequest(location.Id, guests + 2) with
            {
                TableNumber = Random.Shared.Next(10_000, 1_000_000),
            }),
            "Adding the table");
        scope.TrackDelete($"table:{table.Id}", () => tables.DeleteTableAsync(table.Id, waiter.IdToken));

        var start = DateTime.UtcNow.Date.AddDays(Random.Shared.Next(10, 300)).AddHours(12);
        var reservations = new ReservationsApi(actor.UsingTheApi());
        var reservation = await SetUpSteps.ExpectAsync<ReservationResponse>(
            await reservations.CreateReservationAsync(
                new CreateReservationRequest(table.Id, start, start.AddHours(2), guests), owner.IdToken),
            "Booking the table");
        scope.TrackDelete(
            $"reservation:{reservation.Id}",
            () => reservations.CancelReservationAsync(reservation.Id, owner.IdToken));

        var groups = new GroupOrdersApi(actor.UsingTheApi());
        var token = await SetUpSteps.ExpectAsync<InviteTokenResponse>(
            await groups.GetInviteTokenAsync(reservation.Id, owner.IdToken), "Asking for the invitation");
        var invite = await SetUpSteps.ExpectAsync<GroupInviteResponse>(
            await groups.GetInviteAsync(token.Token), "Opening the invitation");

        var group = new GroupInPlay(reservation.Id, invite.GroupReservationId, location.Id);
        if (name is null)
        {
            actor.Remember(group);
            return;
        }

        // Remembering under a name also makes it the current one; a further group must not replace the first.
        var hadCurrent = actor.TryRecall<GroupInPlay>(out var current);
        actor.Remember(name, group);
        if (hadCurrent)
            actor.Remember(current);
    }
}

/// <summary>A guest joins the group through their invitation, optionally picks dishes and submits the cart.</summary>
public sealed class JoinTheGroup(
    string guest,
    IReadOnlyList<(string Dish, int Quantity)> dishes,
    bool submit,
    string? group = null) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var inGroup = actor.Recall<GroupInPlay>(group);
        var api = new GroupOrdersApi(actor.UsingTheApi());
        var session = await SetUpSteps.ExpectAsync<GuestSessionResponse>(
            await api.JoinAsync(new JoinGroupRequest(inGroup.GroupId, guest)), $"{guest} joining the group");
        var joined = new GuestInGroup(guest, session.SessionId, session.SessionToken);
        actor.Remember(guest, joined);

        foreach (var (dish, quantity) in dishes)
            await actor.AttemptsToAsync(new AddToTheCart(guest, dish, quantity));

        if (submit)
            await SetUpSteps.ExpectOkAsync(
                await api.SubmitCartAsync(joined.SessionId, joined.Token), $"{guest} submitting the cart");
    }
}

/// <summary>A guest who already joined adds a dish to their cart.</summary>
public sealed class AddToTheCart(string guest, string dish, int quantity) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var joined = actor.Recall<GuestInGroup>(guest);
        var request = new GuestDishRequest(actor.Recall<DishResponse>(dish).Id, quantity);

        await SetUpSteps.ExpectOkAsync(
            await new GroupOrdersApi(actor.UsingTheApi()).AddDishAsync(joined.SessionId, request, joined.Token),
            $"{guest} adding {dish}");
    }
}

/// <summary>The Owner starts their own pre-order, which the consolidated order is later added to.</summary>
public sealed class StartTheOwnersPreOrder(string dish) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var request = new CreateOrderRequest(
            actor.Recall<GroupInPlay>().ReservationId,
            [new OrderItemRequest(actor.Recall<DishResponse>(dish).Id, 1)]);

        await SetUpSteps.ExpectOkAsync(
            await new OrdersApi(actor.UsingTheApi()).CreateOrderAsync(request, actor.Recall<CustomerSession>("Owner").IdToken),
            "Starting the Owner's pre-order");
    }
}
