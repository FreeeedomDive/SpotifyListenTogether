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

namespace Core.Commands.NextTrack;

public class NextTrackCommand : CommandBase, ICommandWithSpotifyAuth, ICommandForAllParticipants, INextTrackCommand
{
    public NextTrackCommand(
        ITelegramBotClient telegramBotClient,
        ISessionsService sessionsService,
        ISpotifyProfilesService spotifyProfilesService,
        ISpotifyHelpersApiClient spotifyHelpersApiClient,
        IWhitelistService whitelistService,
        ILogger<NextTrackCommand> logger
    ) : base(telegramBotClient, sessionsService, spotifyProfilesService, spotifyHelpersApiClient, whitelistService, logger)
    {
    }

    public Session Session { get; set; } = null!;
    public Dictionary<long, (SessionParticipant Participant, Guid ProfileId)> UserIdToSpotifyProfile { get; set; } = null!;
    public Guid SpotifyProfileId { get; set; }

    protected override async Task ExecuteAsync()
    {
        var result = await this.ApplyToAllParticipants(
            (profileId, _) => SpotifyHelpersApiClient.PlayerV2.NextAsync(profileId, new SpotifyDeviceRequestDto()),
            Logger);
        await NotifyAllAsync(Session, $"{UserName} переключает воспроизведение на следующий трек в очереди\n{result.ToFormattedString()}");
    }
}
