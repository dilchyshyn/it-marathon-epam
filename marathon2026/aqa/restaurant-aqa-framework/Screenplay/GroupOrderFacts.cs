using System.Text.RegularExpressions;
using Restaurant_Aqa_Framework.Api.Seeding;

namespace Restaurant_Aqa_Framework.Screenplay;

/// <summary>A signed-in customer of the scenario, remembered under the role they play ("Owner").</summary>
public sealed record CustomerSession(string Email, string Password, string FirstName, string IdToken);

/// <summary>A reservation with its group; the scenario's current one, or a named one when it has several.</summary>
public sealed record GroupInPlay(string ReservationId, string GroupId, string LocationId);

/// <summary>A guest who joined a group, remembered under their name.</summary>
public sealed record GuestInGroup(string Name, string SessionId, string Token);

/// <summary>One guest as the Owner sees them; the same shape whether it was read from the API or the page.</summary>
public sealed record GuestLine(string Guest, string Dishes, decimal Total, string Cart);

/// <summary>What the Owner sees of the group: its guests and the grand total.</summary>
public sealed record GroupOrderView(IReadOnlyList<GuestLine> Guests, decimal Total)
{
    public static string Describe(IEnumerable<(string Dish, int Quantity)> items)
    {
        var lines = items.Select(i => $"{MenuName.Abbreviate(i.Dish)} x {i.Quantity}").Order(StringComparer.Ordinal).ToList();
        return lines.Count == 0 ? "none" : string.Join(", ", lines);
    }
}

/// <summary>Dishes get a unique name per scenario, so the shared menu never decides a result; scenarios refer to
/// them by the short name written in the feature file.</summary>
public static partial class MenuName
{
    public static string Unique(string shortName) => $"{SampleDataFactory.Prefix}{shortName}-{Guid.NewGuid():N}"[..40];

    public static string Abbreviate(string fullName)
    {
        var match = ShortNamePattern().Match(fullName);
        return match.Success ? match.Groups[1].Value : fullName;
    }

    [GeneratedRegex("^AQA-Sample-(.+?)-[0-9a-f]+$")]
    private static partial Regex ShortNamePattern();
}
