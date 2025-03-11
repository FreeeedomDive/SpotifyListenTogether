using SpotifyAPI.Web;

namespace Core.Extensions;

public static class FormattingExtensions
{
    public static string ToTrackUri(this string track)
    {
        return $"{TrackUriPrefix}{track}";
    }

    public static string GetIdFromTrackUri(this string trackUri)
    {
        return trackUri[TrackUriPrefix.Length..];
    }

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

    public static string ToFormattedString(this Context context)
    {
        return $"[{context.Type.Escape()}]({context.ExternalUrls["spotify"]})";
    }

    /// <summary>
    ///     Escape reserved characters in Telegram MarkdownV2 format
    /// </summary>
    public static string Escape(this string str)
    {
        return str.Replace("-", "\\-")
                  .Replace("(", "\\(")
                  .Replace(")", "\\)")
                  .Replace(".", "\\.")
                  .Replace("+", "\\+")
                  .Replace("!", "\\!")
                  .Replace("=", "\\=")
                  .Replace("<", "\\<")
                  .Replace(">", "\\>")
                  .Replace("[", "\\[")
                  .Replace("]", "\\]");
    }

    private const string TrackUriPrefix = "spotify:track:";
}