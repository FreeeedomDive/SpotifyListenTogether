using System.Text;
using Core.Commands.Base;
using Core.Commands.Base.Interfaces;
using Core.Extensions;
using Core.Sessions;
using Core.Sessions.Models;
using Core.Spotify.Auth.Storage;
using Core.Whitelist;
using Microsoft.Extensions.Logging;
using SpotifyHelpers.Api.Client;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace Core.Commands.SessionInfo;

public class SessionInfoCommand : CommandBase, ICommandWithSpotifyAuth, ICommandForAllParticipants, ISessionInfoCommand
{
    public SessionInfoCommand(
        ITelegramBotClient telegramBotClient,
        ISessionsService sessionsService,
        ISpotifyProfilesService spotifyProfilesService,
        ISpotifyHelpersApiClient spotifyHelpersApiClient,
        IWhitelistService whitelistService,
        ILogger<SessionInfoCommand> logger
    ) : base(telegramBotClient, sessionsService, spotifyProfilesService, spotifyHelpersApiClient, whitelistService, logger)
    {
    }

    public Session Session { get; set; } = null!;
    public Dictionary<long, (SessionParticipant Participant, Guid ProfileId)> UserIdToSpotifyProfile { get; set; } = null!;
    public Guid SpotifyProfileId { get; set; }

    protected override async Task ExecuteAsync()
    {
        var sessionIdTitle = $"*Сессия* `{Session.Id}`";
        const string savedPlaybackTitle = "Последний сохраненный плейбэк";
        var savedPlayback = await GetSavedPlaybackContent();
        var tasks = UserIdToSpotifyProfile.Select(
            async pair =>
            {
                var participant = pair.Value.Participant;
                var profileId = pair.Value.ProfileId;

                var responseBuilder = new StringBuilder().AppendLine($"*{participant.UserName}*");
                var spotifyCurrentlyPlaying = await SpotifyHelpersApiClient.PlayerV2.GetCurrentlyPlayingAsync(profileId);
                if (!spotifyCurrentlyPlaying.IsActive || spotifyCurrentlyPlaying.Track is not { } spotifyCurrentlyPlayingTrack)
                {
                    return responseBuilder.Append("Сейчас ничего не слушает").ToString();
                }

                var currentPlayback = await SpotifyHelpersApiClient.PlayerV2.GetPlaybackAsync(profileId);
                var device = currentPlayback.Device;
                var context = currentPlayback.Context;

                return responseBuilder
                       .Append(spotifyCurrentlyPlayingTrack.ToFormattedString())
                       .AppendLine($" - {FormatTime(currentPlayback.ProgressMs)}".Escape())
                       .AppendLine($"Контекст: {(context is null ? "null" : context.ToFormattedString())}")
                       .AppendLine($"Устройство: {device?.Name ?? "none"} ({device?.Id ?? "none"})".Escape())
                       .Append($"Сохраненное устройство: {participant.DeviceId ?? "none"}")
                       .ToString();
            }
        );
        var playbackInfos = await Task.WhenAll(tasks);
        var messageParts = new List<string>();
        messageParts.Add(sessionIdTitle);
        messageParts.AddRange(playbackInfos);
        messageParts.Add($"{savedPlaybackTitle}\n{savedPlayback}");
        await SendResponseAsync(UserId, string.Join("\n\n", messageParts), ParseMode.MarkdownV2);
    }

    private async Task<string> GetSavedPlaybackContent()
    {
        if (Session.Context?.TrackUri is null)
        {
            return "Нет сохраненных треков";
        }

        var track = await SpotifyHelpersApiClient.Metadata.TryGetTrackAsync(Session.Context.TrackUri.GetIdFromTrackUri());
        if (track is null)
        {
            return "Нет сохраненных треков";
        }

        return $"{track.ToFormattedString()} "
               + $"{FormatTime(Session.Context.PositionMs ?? 0)}".Escape();
    }

    private static string FormatTime(int positionMs)
    {
        return $@"{TimeSpan.FromMilliseconds(positionMs):m\:ss\.fff}";
    }
}
