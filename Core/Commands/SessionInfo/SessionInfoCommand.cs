using System.Text;
using Core.Commands.Base;
using Core.Commands.Base.Interfaces;
using Core.Extensions;
using Core.Sessions;
using Core.Sessions.Models;
using Core.Spotify.Client;
using Core.Whitelist;
using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace Core.Commands.SessionInfo;

public class SessionInfoCommand : CommandBase, ICommandWithSpotifyAuth, ICommandForAllParticipants, ISessionInfoCommand
{
    public SessionInfoCommand(
        ITelegramBotClient telegramBotClient,
        ISessionsService sessionsService,
        ISpotifyClientStorage spotifyClientStorage,
        ISpotifyClientFactory spotifyClientFactory,
        IWhitelistService whitelistService,
        ILogger<SessionInfoCommand> logger
    ) : base(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, whitelistService, logger)
    {
    }

    public Session Session { get; set; } = null!;
    public Dictionary<long, (SessionParticipant Participant, ISpotifyClient SpotifyClient)> UserIdToSpotifyClient { get; set; } = null!;
    public ISpotifyClient SpotifyClient { get; set; } = null!;

    protected override async Task ExecuteAsync()
    {
        var tasks = UserIdToSpotifyClient.Select(
            async pair =>
            {
                var participant = pair.Value.Participant;
                var spotifyClient = pair.Value.SpotifyClient;

                var responseBuilder = new StringBuilder().AppendLine($"*{participant.UserName}*");
                var spotifyCurrentlyPlaying = await spotifyClient.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest());
                // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract - spotifyCurrentlyPlaying actually CAN BE null
                if (spotifyCurrentlyPlaying?.Item is not FullTrack spotifyCurrentlyPlayingTrack)
                {
                    return responseBuilder.Append("Сейчас ничего не слушает").ToString();
                }

                var currentPlayback = await spotifyClient.Player.GetCurrentPlayback();
                var device = currentPlayback.Device;
                var context = currentPlayback.Context;

                return responseBuilder
                       .Append(spotifyCurrentlyPlayingTrack.ToFormattedString())
                       .AppendLine($@" - {TimeSpan.FromMilliseconds(currentPlayback.ProgressMs):m\:ss\.fff}".Escape())
                       // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract - context actually CAN BE null
                       .AppendLine($"Контекст: {(context is null ? "null" : context.ToFormattedString())}")
                       .AppendLine($"Устройство: {device.Name} ({device.Id})".Escape())
                       .Append($"Сохраненное устройство: {participant.DeviceId ?? "none"}")
                       .ToString();
            }
        );
        var playbackInfos = await Task.WhenAll(tasks);
        await SendResponseAsync(UserId, string.Join("\n\n", playbackInfos), ParseMode.MarkdownV2);
    }
}