using Core.Sessions;
using SpotifyAPI.Web;

namespace Core.Commands.Base.Interfaces;

public interface ICommandForAllParticipants : ICommandWithSession
{
    Dictionary<long, (SessionParticipant Participant, ISpotifyClient SpotifyClient)> UserIdToSpotifyClient { get; set; }
}