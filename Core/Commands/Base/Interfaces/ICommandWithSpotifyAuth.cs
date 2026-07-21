namespace Core.Commands.Base.Interfaces;

public interface ICommandWithSpotifyAuth
{
    Guid SpotifyProfileId { get; set; }
}
