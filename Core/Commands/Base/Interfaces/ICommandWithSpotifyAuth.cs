using SpotifyAPI.Web;

namespace Core.Commands.Base.Interfaces;

public interface ICommandWithSpotifyAuth
{
    ISpotifyClient SpotifyClient { get; set; }
}