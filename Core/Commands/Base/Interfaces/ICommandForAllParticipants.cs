using Core.Sessions;
using Core.Sessions.Models;

namespace Core.Commands.Base.Interfaces;

public interface ICommandForAllParticipants : ICommandWithSession
{
    Dictionary<long, (SessionParticipant Participant, Guid ProfileId)> UserIdToSpotifyProfile { get; set; }
}
