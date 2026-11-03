using Microsoft.Playwright;
using Restaurant_Aqa_Framework.Api.Dtos.GroupOrders;

namespace Restaurant_Aqa_Framework.Api;

/// <summary>The group pre-order: the invite and the guests' carts (guest session token) and the Owner's
/// view, submit and removal (Cognito id token).</summary>
public sealed class GroupOrdersApi(IAPIRequestContext api) : ApiClientBase(api)
{
    public Task<IAPIResponse> GetInviteTokenAsync(string reservationId, string? bearerToken) =>
        GetAsync(Url($"/reservations/{reservationId}/invite-token"), Authorized(bearerToken));

    public Task<IAPIResponse> GetInviteAsync(string token) =>
        GetAsync(Url($"/guest/invites/{Uri.EscapeDataString(token)}"));

    public Task<IAPIResponse> JoinAsync(JoinGroupRequest request) =>
        Api.PostAsync(Url("/guest/sessions"), new APIRequestContextOptions
        {
            DataString = Serialize(request),
            Headers = Headers(),
        });

    public Task<IAPIResponse> AddDishAsync(string sessionId, GuestDishRequest request, string token) =>
        Api.PostAsync(Url($"/guest/sessions/{sessionId}/dishes"), new APIRequestContextOptions
        {
            DataString = Serialize(request),
            Headers = Headers(token),
        });

    public Task<IAPIResponse> SubmitCartAsync(string sessionId, string token) =>
        Api.PostAsync(Url($"/guest/sessions/{sessionId}/submit"), Authorized(token));

    public Task<IAPIResponse> GetSessionsAsync(string groupId, string? bearerToken) =>
        GetAsync(Url($"/guest/groups/{groupId}/sessions"), Authorized(bearerToken));

    public Task<IAPIResponse> GetStatusAsync(string groupId) =>
        GetAsync(Url($"/guest/groups/{groupId}/status"));

    public Task<IAPIResponse> SubmitGroupAsync(string groupId, string reservationId, string? bearerToken) =>
        Api.PostAsync(Url($"/group-reservations/{groupId}/submit"), new APIRequestContextOptions
        {
            DataString = Serialize(new SubmitGroupRequest(reservationId)),
            Headers = Headers(bearerToken),
        });

    public Task<IAPIResponse> RemoveSessionAsync(string groupId, string sessionId, string? bearerToken) =>
        DeleteAsync(Url($"/guest/groups/{groupId}/sessions/{sessionId}"), Authorized(bearerToken));

    public Task<IAPIResponse> AddWaiterAsync(string email, string locationId) =>
        Api.PostAsync(Url("/waiters"), new APIRequestContextOptions
        {
            DataString = Serialize(new WaiterRequest(email, locationId)),
            Headers = Headers(),
        });

    public Task<IAPIResponse> RemoveWaiterAsync(string email, string locationId) =>
        DeleteAsync(Url("/waiters"), new APIRequestContextOptions
        {
            DataString = Serialize(new WaiterRequest(email, locationId)),
            Headers = Headers(),
        });

    private static APIRequestContextOptions Authorized(string? bearerToken) =>
        new() { Headers = Headers(bearerToken) };
}
