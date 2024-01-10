using Core.Sessions;

namespace Core.Extensions;

public static class SpotifyOperationResultExtensions
{
    public static string ToFormattedString(this (SessionParticipant participant, bool success)[] results)
    {
        return string.Join("\n", results.Select(x => $"{x.participant.UserName}: {(x.success ? "✔️" : "❌")}")).Escape();
    }
}