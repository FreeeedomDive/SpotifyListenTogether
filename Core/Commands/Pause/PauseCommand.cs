using Core.Commands.Base;
using Core.Commands.Base.Interfaces;
using Core.Extensions;
using Core.Sessions;
using Core.Sessions.Models;
using Core.Spotify.Auth.Storage;
using Core.Whitelist;
using Microsoft.Extensions.Logging;
using SpotifyHelpers.Api.Client;
using SpotifyHelpers.Dto.Spotify;
using Telegram.Bot;

namespace Core.Commands.Pause;

public class PauseCommand : CommandBase, ICommandWithSpotifyAuth, ICommandForAllParticipants, ICommandCanSaveSpotifyDeviceId, IPauseCommand
{
    public PauseCommand(
        ITelegramBotClient telegramBotClient,
        ISessionsService sessionsService,
        ISpotifyProfilesService spotifyProfilesService,
        ISpotifyHelpersApiClient spotifyHelpersApiClient,
        IWhitelistService whitelistService,
        ILogger<PauseCommand> logger
    ) : base(telegramBotClient, sessionsService, spotifyProfilesService, spotifyHelpersApiClient, whitelistService, logger)
    {
    }

    public Dictionary<long, (SessionParticipant Participant, Guid ProfileId)> UserIdToSpotifyProfile { get; set; } = null!;
    public Session Session { get; set; } = null!;
    public Guid SpotifyProfileId { get; set; }

    protected override async Task ExecuteAsync()
    {
        var playback = await SpotifyHelpersApiClient.PlayerV2.GetPlaybackAsync(SpotifyProfileId);
        var result = await this.ApplyToAllParticipants(
            async (profileId, participant) =>
            {
                await SpotifyHelpersApiClient.PlayerV2.PauseAsync(profileId, new SpotifyDeviceRequestDto());
                await this.SaveDeviceIdAsync(SpotifyHelpersApiClient.PlayerV2, profileId, participant);
            }, Logger
        );
        await NotifyAllAsync(Session, $"{UserName} ставит воспроизведение на паузу\n{result.ToFormattedString()}");
        try
        {
            if (playback.Context?.Uri is not { } contextUri || playback.Track?.Uri is not { } trackUri)
            {
                return;
            }

            Session.Context = new SessionContext
            {
                ContextUri = contextUri,
                TrackUri = trackUri,
                PositionMs = playback.ProgressMs,
            };
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to save context");
        }
    }
}
