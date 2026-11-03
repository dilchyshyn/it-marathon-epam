using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Screenplay.Questions;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>Someone asks for the group's participants; the outcome is the scenario's last response.</summary>
public sealed class AskForTheGroupOrder(string persona, string? groupId = null) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var token = await actor.AsksForAsync(new TheTokenOf(persona));
        var id = groupId ?? actor.Recall<GroupInPlay>().GroupId;
        await actor.TheLastResponse().CaptureAsync(await new GroupOrdersApi(actor.UsingTheApi()).GetSessionsAsync(id, token));
    }
}

/// <summary>Someone submits the consolidated pre-order; the outcome is the scenario's last response.</summary>
public sealed class SubmitTheGroupOrder(string persona, string? groupId = null, string? reservationId = null, string? group = null) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var token = await actor.AsksForAsync(new TheTokenOf(persona));
        var inPlay = actor.Recall<GroupInPlay>(group);
        await actor.TheLastResponse().CaptureAsync(
            await new GroupOrdersApi(actor.UsingTheApi()).SubmitGroupAsync(
                groupId ?? inPlay.GroupId, reservationId ?? inPlay.ReservationId, token));
    }
}

/// <summary>Someone removes a participant from the group; the outcome is the scenario's last response.</summary>
public sealed class RemoveFromTheGroup(string persona, string sessionId) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var token = await actor.AsksForAsync(new TheTokenOf(persona));
        await actor.TheLastResponse().CaptureAsync(
            await new GroupOrdersApi(actor.UsingTheApi()).RemoveSessionAsync(actor.Recall<GroupInPlay>().GroupId, sessionId, token));
    }
}

/// <summary>A guest tries to submit their cart; the outcome is the scenario's last response.</summary>
public sealed class TryToSubmitTheCart(string guest) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var joined = actor.Recall<GuestInGroup>(guest);
        await actor.TheLastResponse().CaptureAsync(
            await new GroupOrdersApi(actor.UsingTheApi()).SubmitCartAsync(joined.SessionId, joined.Token));
    }
}
