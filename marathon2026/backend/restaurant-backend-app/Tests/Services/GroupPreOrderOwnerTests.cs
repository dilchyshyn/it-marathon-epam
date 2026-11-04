using Amazon.CognitoIdentityProvider.Model;
using APIHandler.Constants;
using APIHandler.Constants.Dishes;
using APIHandler.Constants.Orders;
using APIHandler.Entities;
using APIHandler.Exceptions;
using APIHandler.Models.GuestSessions;
using APIHandler.Repositories.Auth;
using APIHandler.Repositories.Dishes;
using APIHandler.Repositories.GroupReservations;
using APIHandler.Repositories.GuestSessions;
using APIHandler.Repositories.Orders;
using APIHandler.Repositories.Reservations;
using APIHandler.Repositories.Tables;
using APIHandler.Services.GuestSessions;
using Moq;

namespace APIHandler.Tests.Services;

public class GroupPreOrderOwnerTests
{
    private const string OwnerId = "owner-user";
    private const string ReservationId = "reservation-1";
    private const string GroupId = "group-1";

    private readonly Mock<IGuestSessionRepository> _guestSessionRepository = new();
    private readonly Mock<IGroupReservationRepository> _groupReservationRepository = new();
    private readonly Mock<IDishRepository> _dishRepository = new();
    private readonly Mock<IReservationRepository> _reservationRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ITableRepository> _tableRepository = new();
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly GuestSessionService _service;

    private readonly GroupReservation _group = new()
    {
        GroupReservationId = GroupId,
        ReservationId = ReservationId,
        ReservationToken = "token",
        Status = GroupReservationStatus.Open,
    };

    private readonly Reservation _reservation = new()
    {
        Id = ReservationId,
        TableId = "table-1",
        WaiterId = "waiter-1",
        CustomerId = OwnerId,
        GuestCount = 4,
        Status = ReservationStatus.Reserved,
    };

    private static readonly Dish Caesar = CreateDish("dish1", "Caesar Salad", 180);
    private static readonly Dish TomYum = CreateDish("dish2", "Tom Yum", 220);

    public GroupPreOrderOwnerTests()
    {
        _service = new GuestSessionService(
            _groupReservationRepository.Object,
            _guestSessionRepository.Object,
            _dishRepository.Object,
            _reservationRepository.Object,
            _userRepository.Object,
            _tableRepository.Object,
            _orderRepository.Object);

        _groupReservationRepository.Setup(r => r.GetGroupReservationByIdAsync(GroupId)).ReturnsAsync(_group);
        _groupReservationRepository.Setup(r => r.GetByReservationIdAsync(ReservationId)).ReturnsAsync(_group);
        _reservationRepository.Setup(r => r.GetReservationByIdAsync(ReservationId)).ReturnsAsync(_reservation);

        _dishRepository
            .Setup(r => r.GetDishesByIdsAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync((IEnumerable<string> ids) => new[] { Caesar, TomYum }.Where(d => ids.Contains(d.Id)));

        var owner = new UserType { Attributes = [new AttributeType { Name = "given_name", Value = "Yevhenii" }] };
        _userRepository.Setup(r => r.GetUserByIdAsync(OwnerId)).ReturnsAsync(owner);
    }

    // ---------- Level 1: GET /guest/groups/{groupReservationId}/sessions ----------

    [Fact]
    public async Task GetGroupSessionsAsync_Owner_ReturnsParticipantsWithTotals()
    {
        SetupSessions(
            Session("session1", "Andrii", submitted: true, ("dish1", 2)),
            Session("session2", "Olena", submitted: false, ("dish2", 1)),
            OwnerSession(("dish1", 1)));

        GetAllSessionsResponse result = await _service.GetGroupSessionsAsync(OwnerId, GroupId);

        var participants = result.Participants.ToList();
        Assert.Equal(3, participants.Count);

        var owner = participants[0];
        Assert.Equal(ParticipantType.Owner, owner.Type);
        Assert.Equal("Yevhenii", owner.Name);
        Assert.Equal(180, owner.Total);

        var andrii = participants.Single(p => p.Id == "session1");
        Assert.Equal(ParticipantType.Guest, andrii.Type);
        Assert.Equal(360, andrii.Total);
        Assert.True(andrii.IsSubmitted);
        Assert.Equal("Caesar Salad", andrii.Items.Single().Dish.Name);
        Assert.Equal(2, andrii.Items.Single().Quantity);

        var olena = participants.Single(p => p.Id == "session2");
        Assert.Equal(220, olena.Total);
        Assert.False(olena.IsSubmitted);

        Assert.Equal(760, result.Total);
    }

    [Fact]
    public async Task GetGroupSessionsAsync_NoSessions_ReturnsEmptySnapshot()
    {
        SetupSessions();

        GetAllSessionsResponse result = await _service.GetGroupSessionsAsync(OwnerId, GroupId);

        Assert.Empty(result.Participants);
        Assert.Equal(0, result.Total);
        _dishRepository.Verify(r => r.GetDishesByIdsAsync(It.IsAny<IEnumerable<string>>()), Times.Never);
    }

    [Fact]
    public async Task GetGroupSessionsAsync_NotOwner_ThrowsForbidden()
    {
        await Assert.ThrowsAsync<ActionForbiddenException>(() => _service.GetGroupSessionsAsync("someone-else", GroupId));
    }

    [Fact]
    public async Task GetGroupSessionsAsync_GroupNotFound_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<GroupReservationNotFoundException>(() => _service.GetGroupSessionsAsync(OwnerId, "missing"));
    }

    // ---------- Level 2: GET /reservations/{reservationId}/guest-session-id ----------

    [Fact]
    public async Task GetOwnerSessionIdAsync_OwnerSessionExists_ReturnsItsId()
    {
        SetupSessions(Session("session1", "Andrii", submitted: false), OwnerSession());

        var result = await _service.GetOwnerSessionIdAsync(OwnerId, ReservationId);

        Assert.Equal("owner-session", result.GuestSessionId);
        _guestSessionRepository.Verify(r => r.SaveAsync(It.IsAny<GuestSession>()), Times.Never);
    }

    [Fact]
    public async Task GetOwnerSessionIdAsync_NoOwnerSession_CreatesOne()
    {
        SetupSessions(Session("session1", "Andrii", submitted: false));
        GuestSession? saved = null;
        _guestSessionRepository.Setup(r => r.SaveAsync(It.IsAny<GuestSession>())).Callback<GuestSession>(s => saved = s);

        var result = await _service.GetOwnerSessionIdAsync(OwnerId, ReservationId);

        Assert.NotNull(saved);
        Assert.True(saved.IsOwnerSession);
        Assert.Equal(OwnerId, saved.OwnerUserId);
        Assert.Equal(GroupId, saved.GroupReservationId);
        Assert.Equal(saved.Id, result.GuestSessionId);
    }

    [Fact]
    public async Task GetOwnerSessionIdAsync_ReservationNotFound_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<ReservationNotFoundException>(() => _service.GetOwnerSessionIdAsync(OwnerId, "missing"));
    }

    [Fact]
    public async Task GetOwnerSessionIdAsync_GroupNotFound_ThrowsNotFound()
    {
        _groupReservationRepository.Setup(r => r.GetByReservationIdAsync(ReservationId)).ReturnsAsync((GroupReservation?)null);

        await Assert.ThrowsAsync<GroupReservationNotFoundException>(() => _service.GetOwnerSessionIdAsync(OwnerId, ReservationId));
    }

    [Fact]
    public async Task GetOwnerSessionIdAsync_NotOwner_ThrowsForbidden()
    {
        await Assert.ThrowsAsync<ActionForbiddenException>(() => _service.GetOwnerSessionIdAsync("someone-else", ReservationId));
    }

    // ---------- Level 2: POST /group-reservations/{groupReservationId}/submit ----------

    [Fact]
    public async Task SubmitGroupPreOrderAsync_MergesSubmittedSessionsAndUpdatesStatuses()
    {
        SetupSessions(
            Session("session1", "Andrii", submitted: true, ("dish1", 2)),
            Session("session2", "Olena", submitted: true, ("dish1", 1), ("dish2", 2)),
            Session("session3", "Ivan", submitted: false, ("dish2", 5)));
        var order = CreateOrder();
        _orderRepository.Setup(r => r.GetOrderByReservationIdAsync(ReservationId)).ReturnsAsync(order);

        var result = (await _service.SubmitGroupPreOrderAsync(OwnerId, GroupId, ReservationId)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(3, result.Single(d => d.DishId == "dish1").Quantity);
        Assert.Equal(2, result.Single(d => d.DishId == "dish2").Quantity);

        Assert.Equal(OrderStatus.SubmittedByCustomer, order.Status);
        Assert.Equal(3, order.Dishes.Single(d => d.DishId == "dish1").Quantity);
        Assert.Equal(2, order.Dishes.Single(d => d.DishId == "dish2").Quantity);
        _orderRepository.Verify(r => r.SaveOrderAsync(order), Times.Once);

        Assert.Equal(GroupReservationStatus.Submitted, _group.Status);
        _groupReservationRepository.Verify(r => r.SaveGroupReservationAsync(_group), Times.Once);
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_NoSubmittedSessions_ThrowsBadRequest()
    {
        SetupSessions(Session("session1", "Andrii", submitted: false, ("dish1", 1)));

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.SubmitGroupPreOrderAsync(OwnerId, GroupId, ReservationId));

        Assert.Contains("No submitted guest sessions", exception.Message);
        AssertNothingSaved();
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_SubmittedSessionsWithoutDishes_ThrowsBadRequest()
    {
        SetupSessions(Session("session1", "Andrii", submitted: true));

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.SubmitGroupPreOrderAsync(OwnerId, GroupId, ReservationId));

        Assert.Contains("No dishes", exception.Message);
        AssertNothingSaved();
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_OrderNotFound_ThrowsBadRequest()
    {
        SetupSessions(Session("session1", "Andrii", submitted: true, ("dish1", 1)));
        _orderRepository.Setup(r => r.GetOrderByReservationIdAsync(ReservationId)).ReturnsAsync((Order?)null);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.SubmitGroupPreOrderAsync(OwnerId, GroupId, ReservationId));

        Assert.Contains("pre-order", exception.Message);
        AssertNothingSaved();
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_GroupNotFound_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<GroupReservationNotFoundException>(() => _service.SubmitGroupPreOrderAsync(OwnerId, "missing", ReservationId));
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_ReservationNotFound_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<ReservationNotFoundException>(() => _service.SubmitGroupPreOrderAsync(OwnerId, GroupId, "missing"));
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_NotOwner_ThrowsForbidden()
    {
        await Assert.ThrowsAsync<ActionForbiddenException>(() => _service.SubmitGroupPreOrderAsync("someone-else", GroupId, ReservationId));
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_AlreadySubmitted_ThrowsBadRequest()
    {
        _group.Status = GroupReservationStatus.Submitted;

        await Assert.ThrowsAsync<GroupReservationSubmittedException>(() => _service.SubmitGroupPreOrderAsync(OwnerId, GroupId, ReservationId));
        AssertNothingSaved();
    }

    // ---------- Level 3: DELETE /guest/groups/{groupReservationId}/sessions/{guestSessionId} ----------

    [Fact]
    public async Task DeleteGuestSessionAsync_Guest_ReturnsUpdatedSnapshot()
    {
        var andrii = Session("session1", "Andrii", submitted: true, ("dish1", 2));
        var olena = Session("session2", "Olena", submitted: false, ("dish2", 1));
        _guestSessionRepository.Setup(r => r.GetByGuestSessionId("session1")).ReturnsAsync(andrii);
        SetupSessions(olena);

        GetAllSessionsResponse result = await _service.DeleteGuestSessionAsync(OwnerId, GroupId, "session1");

        _guestSessionRepository.Verify(r => r.DeleteAsync(andrii), Times.Once);
        var participant = Assert.Single(result.Participants);
        Assert.Equal("session2", participant.Id);
        Assert.Equal(220, result.Total);
    }

    [Fact]
    public async Task DeleteGuestSessionAsync_IndexStillListsTheRemovedGuest_LeavesThemOutOfTheSnapshot()
    {
        var andrii = Session("session1", "Andrii", submitted: true, ("dish1", 2));
        var olena = Session("session2", "Olena", submitted: false, ("dish2", 1));
        _guestSessionRepository.Setup(r => r.GetByGuestSessionId("session1")).ReturnsAsync(andrii);

        // The sessions are read through a secondary index, which can lag behind the delete for a moment.
        SetupSessions(andrii, olena);

        GetAllSessionsResponse result = await _service.DeleteGuestSessionAsync(OwnerId, GroupId, "session1");

        var participant = Assert.Single(result.Participants);
        Assert.Equal("session2", participant.Id);
        Assert.Equal(220, result.Total);
    }

    [Fact]
    public async Task DeleteGuestSessionAsync_OwnerSession_ThrowsBadRequest()
    {
        _guestSessionRepository.Setup(r => r.GetByGuestSessionId("owner-session")).ReturnsAsync(OwnerSession());

        await Assert.ThrowsAsync<ValidationException>(() => _service.DeleteGuestSessionAsync(OwnerId, GroupId, "owner-session"));
        _guestSessionRepository.Verify(r => r.DeleteAsync(It.IsAny<GuestSession>()), Times.Never);
    }

    [Fact]
    public async Task DeleteGuestSessionAsync_SessionFromAnotherGroup_ThrowsBadRequest()
    {
        var foreign = Session("session9", "Stranger", submitted: false);
        foreign.GroupReservationId = "other-group";
        _guestSessionRepository.Setup(r => r.GetByGuestSessionId("session9")).ReturnsAsync(foreign);

        await Assert.ThrowsAsync<ValidationException>(() => _service.DeleteGuestSessionAsync(OwnerId, GroupId, "session9"));
        _guestSessionRepository.Verify(r => r.DeleteAsync(It.IsAny<GuestSession>()), Times.Never);
    }

    [Fact]
    public async Task DeleteGuestSessionAsync_SessionNotFound_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<GuestSessionNotFoundException>(() => _service.DeleteGuestSessionAsync(OwnerId, GroupId, "missing"));
    }

    [Fact]
    public async Task DeleteGuestSessionAsync_NotOwner_ThrowsForbidden()
    {
        await Assert.ThrowsAsync<ActionForbiddenException>(() => _service.DeleteGuestSessionAsync("someone-else", GroupId, "session1"));
        _guestSessionRepository.Verify(r => r.DeleteAsync(It.IsAny<GuestSession>()), Times.Never);
    }

    private void SetupSessions(params GuestSession[] sessions)
    {
        _guestSessionRepository
            .Setup(r => r.GetAllByGroupReservationIdAsync(GroupId))
            .ReturnsAsync(sessions.ToList());
    }

    private void AssertNothingSaved()
    {
        _orderRepository.Verify(r => r.SaveOrderAsync(It.IsAny<Order>()), Times.Never);
        _groupReservationRepository.Verify(r => r.SaveGroupReservationAsync(It.IsAny<GroupReservation>()), Times.Never);
    }

    private static GuestSession Session(string id, string name, bool submitted, params (string DishId, int Quantity)[] dishes) => new()
    {
        Id = id,
        GuestName = name,
        GroupReservationId = GroupId,
        IsSubmitted = submitted,
        Dishes = dishes
            .Select(d => new OrderItem { DishId = d.DishId, Quantity = d.Quantity, State = OrderItemState.Pending })
            .ToList(),
    };

    private static GuestSession OwnerSession(params (string DishId, int Quantity)[] dishes)
    {
        var session = Session("owner-session", "Owner", submitted: true, dishes);
        session.IsOwnerSession = true;
        session.OwnerUserId = OwnerId;
        return session;
    }

    private static Order CreateOrder() => new()
    {
        OrderId = "order-1",
        CustomerId = OwnerId,
        ReservationId = ReservationId,
        ReservationAddress = "Rustaveli Avenue 48, Table 7",
        ReservationTime = "Oct 24, 2026, 7:00 p.m. - 8:30 p.m.",
        Dishes = [],
        Status = OrderStatus.Created,
    };

    private static Dish CreateDish(string id, string name, decimal price) => new()
    {
        Id = id,
        Name = name,
        Price = price,
        Description = string.Empty,
        Weight = "300 g",
        Calories = "0",
        Proteins = "0",
        Fats = "0",
        Carbohydrates = "0",
        Vitamins = "",
        DishType = DishType.MainCourse,
        State = DishState.Available,
    };
}
