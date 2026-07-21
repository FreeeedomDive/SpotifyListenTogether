using SpotifyHelpers.Dto.Spotify;

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

    public static string ToFormattedString(this SpotifyTrackDto track)
    {
        var title = string.Join(" - ", new[] { track.Artists?.FirstOrDefault()?.Name, track.Name }
            .Where(x => !string.IsNullOrEmpty(x)));
        return ToMarkdownLink(title, track.Url);
    }

    public static string ToFormattedString(this SpotifyAlbumSummaryDto album)
    {
        var title = string.Join(" - ", new[] { album.Artists?.FirstOrDefault()?.Name, album.Name }
            .Where(x => !string.IsNullOrEmpty(x)));
        return ToMarkdownLink(title, album.Url);
    }

    public static string ToFormattedString(this SpotifyAlbumDetailsDto album)
    {
        return ((SpotifyAlbumSummaryDto)album).ToFormattedString();
    }

    public static string ToFormattedString(this SpotifyPlaylistDto playlist)
    {
        return ToMarkdownLink(playlist.Name ?? string.Empty, playlist.Url);
    }

    public static string ToFormattedString(this SpotifyPlaybackContextDto context)
    {
        return ToMarkdownLink(context.Type ?? string.Empty, context.Url);
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

    private static string ToMarkdownLink(string text, string? url)
    {
        var escapedText = text.Escape();
        return string.IsNullOrWhiteSpace(url) ? escapedText : $"[{escapedText}]({url})";
    }

    private const string TrackUriPrefix = "spotify:track:";
}
