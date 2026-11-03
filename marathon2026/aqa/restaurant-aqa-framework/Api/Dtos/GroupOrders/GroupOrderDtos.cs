namespace Restaurant_Aqa_Framework.Api.Dtos.GroupOrders;

public sealed record WaiterRequest(string Email, string LocationId);

public sealed record InviteTokenResponse(string Token);

public sealed record GroupInviteResponse(string GroupReservationId, string LocationAddress, int MaxGuestCount);

public sealed record JoinGroupRequest(string GroupReservationId, string GuestName);

public sealed record GuestSessionResponse(string SessionId, string SessionToken);

public sealed record GuestDishRequest(string DishId, int Quantity);

public sealed record SubmitGroupRequest(string ReservationId);

public sealed record SubmittedDish(string DishId, int Quantity);

public sealed record GroupStatusResponse(int CurrentParticipants, int MaxCapacity, string Status);

public sealed record GroupDish(string Id, string Name, decimal Price);

public sealed record GroupItem(GroupDish Dish, int Quantity);

public sealed record GroupParticipant(
    string Id,
    string Type,
    string Name,
    IReadOnlyList<GroupItem> Items,
    decimal Total,
    bool IsSubmitted);

public sealed record GroupSnapshot(IReadOnlyList<GroupParticipant> Participants, decimal Total);
