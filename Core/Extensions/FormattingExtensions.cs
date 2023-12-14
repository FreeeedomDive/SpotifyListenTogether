using SpotifyAPI.Web;

namespace Core.Extensions;

public static class FormattingExtensions
{
    public static string ToFormattedString(this FullTrack track)
    {
        return $"[{track.Artists.First().Name} - {track.Name}]({track.ExternalUrls["spotify"]})".EscapeTelegramReservedSymbols();
    }

    public static string ToFormattedString(this FullAlbum album)
    {
        return $"[{album.Artists.First().Name} - {album.Name}]({album.ExternalUrls["spotify"]})".EscapeTelegramReservedSymbols();
    }

    public static string ToFormattedString(this SimpleArtist artist)
    {
        return $"[{artist.Name}]({artist.ExternalUrls["spotify"]})".EscapeTelegramReservedSymbols();
    }

    public static string ToFormattedString(this FullPlaylist playlist)
    {
        return $"[{playlist.Name}]({playlist.ExternalUrls!["spotify"]})".EscapeTelegramReservedSymbols();
    }

    private static string EscapeTelegramReservedSymbols(this string str)
    {
        return str.Replace("-", "\\-")
                  .Replace("(", "\\(")
                  .Replace(")", "\\)")
                  .Replace(".", "\\.");
    }
}