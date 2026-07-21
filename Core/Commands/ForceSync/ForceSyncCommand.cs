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

namespace Core.Commands.ForceSync;

public class ForceSyncCommand : CommandBase, ICommandWithSpotifyAuth, ICommandForAllParticipants, IForceSyncCommand
{
    public ForceSyncCommand(
        ITelegramBotClient telegramBotClient,
        ISessionsService sessionsService,
        ISpotifyProfilesService spotifyProfilesService,
        ISpotifyHelpersApiClient spotifyHelpersApiClient,
        IWhitelistService whitelistService,
        ILogger<ForceSyncCommand> logger
    ) : base(telegramBotClient, sessionsService, spotifyProfilesService, spotifyHelpersApiClient, whitelistService, logger)
    {
    }

    public Dictionary<long, (SessionParticipant Participant, Guid ProfileId)> UserIdToSpotifyProfile { get; set; } = null!;
    public Session Session { get; set; } = null!;
    public Guid SpotifyProfileId { get; set; }

    protected override async Task ExecuteAsync()
    {
        if (UserIdToSpotifyProfile.Count == 0)
        {
            return;
        }

        var allCurrentProgress = await Task.WhenAll(
            UserIdToSpotifyProfile.Values.Select(
                x => SpotifyHelpersApiClient.PlayerV2.GetPlaybackAsync(x.ProfileId))
        );
        var activePlaybacks = allCurrentProgress.Where(x => x.IsActive).ToArray();
        if (activePlaybacks.Length == 0)
        {
            return;
        }

        var minProgress = activePlaybacks.Min(x => x.ProgressMs);
        var result = await this.ApplyToAllParticipants(
            (profileId, _) => SpotifyHelpersApiClient.PlayerV2.SeekAsync(
                profileId,
                new SpotifySeekRequestDto { PositionMs = minProgress }),
            Logger
        );
        await NotifyAllAsync(Session, $"{UserName} сбрасывает прогресс воспроизведения трека до {minProgress} мс\n{result.ToFormattedString()}");
    }
}
