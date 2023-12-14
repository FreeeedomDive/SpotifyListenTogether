using SpotifyAPI.Web;

namespace Core.Extensions;

public static class FormattingExtensions
{
    public static string ToFormattedString(this FullTrack track)
    {
        return $"[{(track.Artists.First().Name + " - " + track.Name).Escape()}]({track.ExternalUrls["spotify"]})";
    }

    public static string ToFormattedString(this FullAlbum album)
    {
        return $"[{(album.Artists.First().Name + " - " + album.Name).Escape()}]({album.ExternalUrls["spotify"]})";
    }

    public static string ToFormattedString(this FullPlaylist playlist)
    {
        return $"[{playlist.Name!.Escape()}]({playlist.ExternalUrls!["spotify"]})";
    }

    public static string Escape(this string str)
    {
        return str.Replace("-", "\\-")
                  .Replace("(", "\\(")
                  .Replace(")", "\\)")
                  .Replace(".", "\\.");
    }
}