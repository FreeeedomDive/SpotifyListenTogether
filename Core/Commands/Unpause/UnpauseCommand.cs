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

namespace Core.Commands.Unpause;

public class UnpauseCommand : CommandBase, ICommandWithSpotifyAuth, ICommandWithAliveDeviceValidation, IUnpauseCommand
{
    public UnpauseCommand(
        ITelegramBotClient telegramBotClient,
        ISessionsService sessionsService,
        ISpotifyProfilesService spotifyProfilesService,
        ISpotifyHelpersApiClient spotifyHelpersApiClient,
        IWhitelistService whitelistService,
        ILogger<UnpauseCommand> logger
    ) : base(telegramBotClient, sessionsService, spotifyProfilesService, spotifyHelpersApiClient, whitelistService, logger)
    {
    }

    public Session Session { get; set; } = null!;
    public Dictionary<long, (SessionParticipant Participant, Guid ProfileId)> UserIdToSpotifyProfile { get; set; } = null!;
    public Guid SpotifyProfileId { get; set; }

    protected override async Task ExecuteAsync()
    {
        var result = await this.ApplyToAllParticipants(
            (profileId, participant) => SpotifyHelpersApiClient.PlayerV2.PlayAsync(
                profileId,
                new SpotifyPlayRequestDto
                {
                    DeviceId = participant.DeviceId,
                    ContextUri = Session.Context?.ContextUri,
                    PositionMs = Session.Context?.ContextUri is null ? null : Session.Context.PositionMs,
                    OffsetUri = Session.Context?.ContextUri is null ? null : Session.Context.TrackUri,
                }),
            Logger
        );
        await NotifyAllAsync(Session, $"{UserName} возобновляет воспроизведение\n{result.ToFormattedString()}");
    }
}
