using Core.Commands.Base.Interfaces;
using Core.Sessions.Models;
using Microsoft.Extensions.Logging;
using SpotifyHelpers.Api.Client.PlayerV2;

namespace Core.Extensions;

public static class CommandsExtensions
{
    public static async Task<(SessionParticipant Participant, bool Result)[]> ApplyToAllParticipants(
        this ICommandForAllParticipants command,
        Func<Guid, SessionParticipant, Task> action,
        ILogger logger
    )
    {
        var profiles = command.UserIdToSpotifyProfile;
        return await Task.WhenAll(
            profiles.Values.Select(
                async x =>
                {
                    try
                    {
                        await action(x.ProfileId, x.Participant);
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
        IPlayerV2Client playerClient,
        Guid profileId,
        SessionParticipant participant
    )
    {
        try
        {
            var playback = await playerClient.GetPlaybackAsync(profileId);
            if (playback.Device?.Id is { } deviceId)
            {
                participant.DeviceId = deviceId;
            }
        }
        catch
        {
            // ignored
        }
    }
}
