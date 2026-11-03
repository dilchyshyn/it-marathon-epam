using Microsoft.Playwright;
using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>The Owner signs in and opens the group order the way they would: from their reservation's card.</summary>
public sealed class OpenTheGroupOrderFromMyReservations : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var owner = actor.Recall<CustomerSession>("Owner");
        await actor.AttemptsToAsync(new SignInThroughTheLoginForm(owner.Email, owner.Password));

        await actor.LookingAtThePage().GetByRole(AriaRole.Link, new() { Name = "Reservations", Exact = true }).ClickAsync();
        await actor.LookingAtThePage()
            .GetByRole(AriaRole.Button, new() { Name = "Group Pre-Order", Exact = true })
            .ClickAsync();

        await new GroupOrderPage(actor.LookingAtThePage()).GrandTotal.WaitForAsync();
    }
}

/// <summary>The Owner refreshes the screen and waits for the new figures.</summary>
public sealed class RefreshTheGroupOrder : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        // The screen applies the sessions and the status together, so a refresh needs both answers.
        var browser = actor.LookingAtThePage();
        var sessions = browser.WaitForResponseAsync(r => r.Url.EndsWith("/sessions", StringComparison.Ordinal) && r.Ok);
        var status = browser.WaitForResponseAsync(r => r.Url.EndsWith("/status", StringComparison.Ordinal) && r.Ok);

        await new GroupOrderPage(browser).RefreshButton.ClickAsync();
        await Task.WhenAll(sessions, status);
    }
}

/// <summary>The Owner submits the pre-order on the screen and confirms the dialog.</summary>
public sealed class SubmitThePreOrderOnTheScreen : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var page = new GroupOrderPage(actor.LookingAtThePage());
        await page.SubmitButton.ClickAsync();
        await page.ConfirmSubmitButton.ClickAsync();
    }
}

/// <summary>The Owner removes a guest on the screen and confirms the dialog.</summary>
public sealed class RemoveTheGuestOnTheScreen(string guest) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var page = new GroupOrderPage(actor.LookingAtThePage());
        await page.RemoveButtonOf(guest).ClickAsync();
        await page.ConfirmRemoveButton.ClickAsync();
        await page.RemoveButtonOf(guest).WaitForAsync(new() { State = WaitForSelectorState.Detached });
    }
}

/// <summary>The waiter signs in (ending the Owner's browser session) and opens the Owner's pre-order.</summary>
public sealed class OpenTheOwnersPreOrderAsTheWaiter : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var waiter = actor.Recall<CustomerSession>("Waiter");
        var page = actor.LookingAtThePage();
        await page.EvaluateAsync("() => { localStorage.clear(); sessionStorage.clear(); }");
        await actor.AttemptsToAsync(new SignInThroughTheLoginForm(waiter.Email, waiter.Password));

        var waiterPage = new WaiterPreOrderPage(page);
        await page.GetByRole(AriaRole.Link, new() { Name = "Reservations", Exact = true }).ClickAsync();
        await waiterPage.ReservationOf(actor.Recall<CustomerSession>("Owner").FirstName)
            .GetByRole(AriaRole.Button, new() { Name = "Pre-order", Exact = true })
            .ClickAsync();
        await waiterPage.DishCards.First.WaitForAsync();
    }
}
