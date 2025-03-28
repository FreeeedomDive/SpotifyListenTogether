using Core.Commands.Base.Interfaces;
using Core.Sessions.Models;
using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;

namespace Core.Extensions;

public static class CommandsExtensions
{
    public static async Task<(SessionParticipant Participant, bool Result)[]> ApplyToAllParticipants(
        this ICommandForAllParticipants command,
        Func<ISpotifyClient, SessionParticipant, Task> action,
        ILogger logger
    )
    {
        var clients = command.UserIdToSpotifyClient;
        return await Task.WhenAll(
            clients.Values.Select(
                async x =>
                {
                    try
                    {
                        await action(x.SpotifyClient, x.Participant);
                        return (x.Participant, true);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Error in spotify action for user {username}", x.Participant.UserName);
                        return (x.Participant, false);
                    }
                }
            )
        );
    }

    public static async Task SaveDeviceIdAsync(
        this ICommandCanSaveSpotifyDeviceId commandCanSaveSpotifyDeviceId,
        ISpotifyClient spotifyClient,
        SessionParticipant participant
    )
    {
        try
        {
            var playback = await spotifyClient.Player.GetCurrentPlayback();
            participant.DeviceId = playback.Device.Id;
        }
        catch
        {
            // ignored
        }
    }
}