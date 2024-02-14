using Core.Commands.Base.Interfaces;
using Core.Sessions;
using SpotifyAPI.Web;
using TelemetryApp.Api.Client.Log;

namespace Core.Extensions;

public static class CommandsExtensions
{
    public static async Task<(SessionParticipant Participant, bool Result)[]> ApplyToAllParticipants(
        this ICommandForAllParticipants command,
        Func<ISpotifyClient, SessionParticipant, Task> action,
        ILoggerClient loggerClient
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
                        await loggerClient.ErrorAsync(e, "Error in spotify action for user {username}", x.Participant.UserName);
                        return (x.Participant, false);
                    }
                }
            )
        );
    }

    public static async Task SaveDeviceIdAsync(
        this ICommandCanSaveSpotifyDeviceId commandCanSaveSpotifyDeviceId,
        ISpotifyClient spotifyClient,
        SessionParticipant participant,
        bool immediately = false
    )
    {
        try
        {
            if (!immediately)
            {
                await Task.Delay(5 * 1000);
            }

            var playback = await spotifyClient.Player.GetCurrentPlayback();
            participant.DeviceId = playback.Device.Id;
        }
        catch
        {
            // ignored
        }
    }
}