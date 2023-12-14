using SpotifyAPI.Web;

namespace Core.Extensions;

public static class FormattingExtensions
{
    public static string ToFormattedString(this FullTrack fullTrack)
    {
        return $"[{fullTrack.Artists.First().Name} - {fullTrack.Name}]({fullTrack.ExternalUrls["spotify"]})".Replace("-", "\\-");
    }
}