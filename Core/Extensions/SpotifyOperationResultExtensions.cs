using Core.Sessions;
using Core.Sessions.Models;

namespace Core.Extensions;

public static class SpotifyOperationResultExtensions
{
    public static string ToFormattedString(this IEnumerable<(SessionParticipant participant, bool success)> results)
    {
        return string.Join("\n", results.Select(x => $"{x.participant.UserName}: {(x.success ? "✔️" : "❌")}")).Escape();
    }
}