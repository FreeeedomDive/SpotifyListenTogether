namespace Core.Spotify.Links;

public interface ISpotifyLinksRecognizeService
{
    Task<SpotifyLink?> TryRecognizeAsync(string link);
}