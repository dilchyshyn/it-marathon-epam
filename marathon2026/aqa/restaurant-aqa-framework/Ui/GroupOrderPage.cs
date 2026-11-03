using Microsoft.Playwright;

namespace Restaurant_Aqa_Framework.Ui;

/// <summary>The Owner's "Group Pre-Order" screen.</summary>
public sealed class GroupOrderPage(IPage page) : BasePage(page)
{
    public ILocator GrandTotal => Page.Locator(".grand-total-value");
    public ILocator ParticipantCards => Page.Locator("app-participant-card");
    public ILocator PendingNotice => Page.Locator(".notice");
    public ILocator EmptyMessage => Page.GetByText("Participants haven’t added any dishes yet");
    public ILocator ConsolidatedItems => Page.Locator(".consolidated-list li");
    public ILocator SentNote => Page.Locator(".sent-note");
    public ILocator SubmitButton => Page.GetByRole(AriaRole.Button, new() { Name = "Submit Pre-Order", Exact = true });
    public ILocator RefreshButton => Page.GetByRole(AriaRole.Button, new() { Name = "Refresh", Exact = true });
    public ILocator ConfirmSubmitButton => Page.Locator("app-modal").GetByRole(AriaRole.Button, new() { Name = "Submit", Exact = true });
    public ILocator ConfirmRemoveButton => Page.Locator("app-modal").GetByRole(AriaRole.Button, new() { Name = "Remove Guest", Exact = true });
    public ILocator ErrorToast => Page.Locator(".toast-error");

    public ILocator RemoveButtonOf(string guest) =>
        Page.GetByRole(AriaRole.Button, new() { Name = $"Remove {guest}", Exact = true });
}

/// <summary>The waiter's reservation list and the pre-order screen it leads to.</summary>
public sealed class WaiterPreOrderPage(IPage page) : BasePage(page)
{
    public ILocator DishCards => Page.Locator(".preorder-list app-dish-card");

    public ILocator ReservationOf(string customerName) =>
        Page.Locator("ul.reservation-list li.card").Filter(new() { HasText = customerName });
}
