using Reqnroll;
using Restaurant_Aqa_Framework.Api.Dtos.Orders;
using Restaurant_Aqa_Framework.Screenplay;
using Restaurant_Aqa_Framework.Screenplay.Questions;
using Restaurant_Aqa_Framework.Screenplay.Tasks;
using Restaurant_Aqa_Framework.Ui;
using System.Globalization;

namespace Restaurant_Aqa_Framework.StepDefinitions;

/// <summary>The group pre-order: everything both stages share, plus what the service says.</summary>
[Binding]
public sealed class GroupOrderStepDefinitions(Actor actor) : StepDefinitionBase(actor)
{
    private const string UnknownId = "00000000-0000-0000-0000-000000000000";

    [Given("a restaurant is open with this menu:")]
    public Task GivenARestaurantIsOpen(DataTable dishes) =>
        Actor.AttemptsToAsync(new OpenARestaurant(
            dishes.Rows.Select(r => (r["dish"], decimal.Parse(r["price"], CultureInfo.InvariantCulture))).ToList()));

    [Given("a customer owns a group reservation for {int} people")]
    public Task GivenACustomerOwnsAGroup(int people) =>
        Actor.AttemptsToAsync(new RegisterACustomer("Owner"), new BookAGroupReservation(people));

    [Given("the owner also has a second group")]
    public Task GivenASecondGroup() => Actor.AttemptsToAsync(new BookAGroupReservation(3, "second"));

    [Given("{string} joined the second group")]
    public Task GivenJoinedTheSecondGroup(string guest) =>
        Actor.AttemptsToAsync(new JoinTheGroup(guest, [], submit: false, group: "second"));

    [Given("these guests have joined:")]
    public async Task GivenTheseGuestsHaveJoined(DataTable guests)
    {
        foreach (var guest in guests.Rows.GroupBy(r => r["guest"]))
        {
            var dishes = guest
                .Where(r => r["dish"].Length > 0)
                .Select(r => (r["dish"], int.Parse(r["quantity"], CultureInfo.InvariantCulture)))
                .ToList();
            await Actor.AttemptsToAsync(new JoinTheGroup(guest.Key, dishes, guest.First()["cart"] == "submitted"));
        }
    }

    [Given("the owner has started their own pre-order with {string}")]
    public Task GivenTheOwnerStartedAPreOrder(string dish) => Actor.AttemptsToAsync(new StartTheOwnersPreOrder(dish));

    [When("{string} adds {int} more of {string} to their cart")]
    public Task WhenGuestAddsMore(string guest, int quantity, string dish) =>
        Actor.AttemptsToAsync(new AddToTheCart(guest, dish, quantity));

    [When("{word} {word} asks to see the group order")]
    public Task WhenSomeoneAsksToSee(string article, string who) =>
        Actor.AttemptsToAsync(new AskForTheGroupOrder($"{article} {who}"));

    [When("an anonymous visitor asks to see the group order")]
    public Task WhenAnonymousAsksToSee() => Actor.AttemptsToAsync(new AskForTheGroupOrder("an anonymous visitor"));

    [When("the owner asks to see a group that does not exist")]
    public Task WhenTheOwnerAsksForUnknownGroup() => Actor.AttemptsToAsync(new AskForTheGroupOrder("the owner", UnknownId));

    [When("{word} {word} submits the group pre-order")]
    public Task WhenSomeoneSubmits(string article, string who) =>
        Actor.AttemptsToAsync(new SubmitTheGroupOrder($"{article} {who}"));

    [When("an anonymous visitor submits the group pre-order")]
    public Task WhenAnonymousSubmits() => Actor.AttemptsToAsync(new SubmitTheGroupOrder("an anonymous visitor"));

    [When("the owner submits a group that does not exist")]
    public Task WhenTheOwnerSubmitsUnknownGroup() => Actor.AttemptsToAsync(new SubmitTheGroupOrder("the owner", groupId: UnknownId));

    [When("the owner submits the group for a reservation that does not exist")]
    public Task WhenTheOwnerSubmitsForUnknownReservation() =>
        Actor.AttemptsToAsync(new SubmitTheGroupOrder("the owner", reservationId: UnknownId));

    [When("the owner removes a guest who does not exist")]
    public Task WhenTheOwnerRemovesUnknown() => Actor.AttemptsToAsync(new RemoveFromTheGroup("the owner", UnknownId));

    [When("{word} {word} removes {string} from the group")]
    public Task WhenSomeoneRemoves(string article, string who, string guest) =>
        Actor.AttemptsToAsync(new RemoveFromTheGroup($"{article} {who}", Recall<GuestInGroup>(guest).SessionId));

    [When("an anonymous visitor removes {string} from the group")]
    public Task WhenAnonymousRemoves(string guest) =>
        Actor.AttemptsToAsync(new RemoveFromTheGroup("an anonymous visitor", Recall<GuestInGroup>(guest).SessionId));

    [When("{string} tries to submit their empty cart")]
    public Task WhenGuestSubmitsEmptyCart(string guest) => Actor.AttemptsToAsync(new TryToSubmitTheCart(guest));

    [Then("the group shows these guests:")]
    [Scope(Tag = "api")]
    public async Task ThenTheServiceShows(DataTable guests) =>
        await AssertGuests(await Actor.AsksForAsync(new TheGroupOrderReported()), guests);

    [Then("the group total is {decimal}")]
    [Scope(Tag = "api")]
    public async Task ThenTheServiceTotalIs(decimal total) =>
        await AssertTotal(await Actor.AsksForAsync(new TheGroupOrderReported()), total);

    [Then("the group has no participants")]
    [Scope(Tag = "api")]
    public async Task ThenTheServiceHasNoParticipants()
    {
        var snapshot = await Actor.AsksForAsync(new TheGroupOrderReported());
        await Assert.That(snapshot.Guests).IsEmpty();
    }

    [Then("the restaurant is sent {string}")]
    public async Task ThenTheRestaurantIsSent(string dishes) =>
        await Assert.That(await Actor.AsksForAsync(new TheDishesSubmitted())).IsEqualTo(dishes);

    [Then("the group is marked as submitted")]
    public async Task ThenTheGroupIsSubmitted() =>
        await Assert.That(await Actor.AsksForAsync(new TheGroupStatus())).IsEqualTo("Submitted");

    [Then("the waiter's order is submitted and holds {string}")]
    public async Task ThenTheWaitersOrderHolds(string dishes)
    {
        var (status, onOrder) = await Actor.AsksForAsync(new TheOrderForTheWaiter());
        await Assert.That(status).IsEqualTo(OrderStatus.SubmittedByCustomer);
        await Assert.That(onOrder).IsEqualTo(dishes);
    }

    [Then("the request is refused because the caller is not signed in")]
    public Task ThenNotSignedIn() => AssertStatus(401);

    [Then("the request is refused because only the owner may do this")]
    public Task ThenOnlyTheOwner() => AssertStatus(403);

    [Then("the group cannot be found")]
    public Task ThenGroupNotFound() => AssertStatus(404);

    [Then("the guest cannot be found")]
    public Task ThenGuestNotFound() => AssertStatus(404);

    [Then("the reservation cannot be found")]
    public Task ThenReservationNotFound() => AssertStatus(404);

    [Then("the request is rejected with the explanation {string}")]
    public async Task ThenRejectedWith(string explanation)
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status).IsEqualTo(400).Because($"response body was: {outcome.Body}");
        await Assert.That(outcome.Body).Contains(explanation);
    }

    private async Task AssertStatus(int status)
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status).IsEqualTo(status).Because($"response body was: {outcome.Body}");
    }

    internal static async Task AssertGuests(GroupOrderView view, DataTable guests)
    {
        var expected = guests.Rows
            .Select(r => new GuestLine(r["guest"], r["dishes"], decimal.Parse(r["total"], CultureInfo.InvariantCulture), r["cart"]))
            .OrderBy(g => g.Guest, StringComparer.Ordinal)
            .ToList();
        var actual = view.Guests.OrderBy(g => g.Guest, StringComparer.Ordinal).ToList();

        await Assert.That(Lines(actual)).IsEqualTo(Lines(expected));
    }

    // "28.00" from the screen and "28" from the feature file are the same amount.
    private static string Lines(IEnumerable<GuestLine> guests) => string.Join(
        " | ",
        guests.Select(g => $"{g.Guest}: {g.Dishes}, {g.Total.ToString("0.##", CultureInfo.InvariantCulture)}, {g.Cart}"));

    internal static async Task AssertTotal(GroupOrderView view, decimal total)
    {
        await Assert.That(view.Total).IsEqualTo(total);
        await Assert.That(view.Guests.Sum(g => g.Total)).IsEqualTo(total).Because("the total is the sum of the participants");
    }
}

/// <summary>What the Owner and the waiter do and see on the screen.</summary>
[Binding]
[Scope(Tag = "ui")]
public sealed class GroupOrderScreenStepDefinitions(Actor actor) : StepDefinitionBase(actor)
{
    private GroupOrderPage Screen => new(Actor.LookingAtThePage());

    [When("the owner opens the group order")]
    public Task WhenTheOwnerOpens() => Actor.AttemptsToAsync(new OpenTheGroupOrderFromMyReservations());

    [When("the owner refreshes the group order")]
    public Task WhenTheOwnerRefreshes() => Actor.AttemptsToAsync(new RefreshTheGroupOrder());

    [When("the owner submits the pre-order on the screen")]
    public Task WhenTheOwnerSubmits() => Actor.AttemptsToAsync(new SubmitThePreOrderOnTheScreen());

    [When("the owner removes {string} on the screen")]
    public Task WhenTheOwnerRemoves(string guest) => Actor.AttemptsToAsync(new RemoveTheGuestOnTheScreen(guest));

    [When("the waiter opens the owner's pre-order")]
    public Task WhenTheWaiterOpens() => Actor.AttemptsToAsync(new OpenTheOwnersPreOrderAsTheWaiter());

    [Then("the group shows these guests:")]
    public async Task ThenTheScreenShows(DataTable guests) =>
        await GroupOrderStepDefinitions.AssertGuests(await Actor.AsksForAsync(new TheGroupOrderShown()), guests);

    [Then("the group total is {decimal}")]
    public async Task ThenTheScreenTotalIs(decimal total)
    {
        // The screen's Grand Total is the consolidated order, so only submitted carts count (FE spec), unlike the
        // service's total, which covers every participant.
        var view = await Actor.AsksForAsync(new TheGroupOrderShown());
        await Assert.That(view.Total).IsEqualTo(total);
        await Assert.That(view.Guests.Where(g => g.Cart == "submitted").Sum(g => g.Total)).IsEqualTo(total);
    }

    [Then("the group has no participants")]
    public async Task ThenTheScreenHasNoParticipants()
    {
        var view = await Actor.AsksForAsync(new TheGroupOrderShown());
        await Assert.That(view.Guests).IsEmpty();
    }

    [Then("the owner is told nobody has chosen any dishes yet")]
    public async Task ThenNobodyHasChosen()
    {
        await Screen.EmptyMessage.WaitForAsync();
        await Assert.That(await Screen.SubmitButton.IsDisabledAsync()).IsTrue();
    }

    [Then("the owner is warned that {int} carts have not been submitted")]
    public async Task ThenTheOwnerIsWarned(int count)
    {
        await Screen.PendingNotice.WaitForAsync();
        var notice = await Screen.PendingNotice.InnerTextAsync();
        await Assert.That(notice).Contains(count == 1 ? "1 participant hasn’t" : $"{count} participants haven’t");
    }

    [Then("the consolidated pre-order holds {string}")]
    public async Task ThenConsolidatedHolds(string dishes) =>
        await Assert.That(await Actor.AsksForAsync(new TheConsolidatedDishesShown())).IsEqualTo(dishes);

    [Then("the owner sees that the pre-order was sent to the restaurant")]
    public Task ThenTheOwnerSeesItWasSent() => Screen.SentNote.WaitForAsync();

    [Then("the owner is told the pre-order could not be sent")]
    public async Task ThenTheOwnerIsToldItFailed()
    {
        await Screen.ErrorToast.WaitForAsync();
        await Assert.That(await Screen.SentNote.CountAsync()).IsEqualTo(0);
    }

    [Then("the waiter sees {string}")]
    public async Task ThenTheWaiterSees(string dishes) =>
        await Assert.That(await Actor.AsksForAsync(new ThePreOrderTheWaiterSees())).IsEqualTo(dishes);
}
