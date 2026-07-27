using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Core.Telemetry;

public static class SltTelemetry
{
    public const string ActivitySourceName = "SpotifyListenTogether";

    public const string MeterName = "SpotifyListenTogether";

    public const string CommandTag = "command";

    public const string OutcomeTag = "outcome";

    public const string ChatIdTag = "slt.chat.id";

    public const string SessionIdTag = "slt.session.id";
    public const string ParticipantsTag = "slt.session.participants";
    public const string ErrorTypeTag = "error.type";

    public const string CommandActivityName = "slt.command";

    public const string UpdateActivityName = "slt.update";

    public const string CommandsCounterName = "slt.commands";
    public const string CommandDurationName = "slt.command.duration";
    public const string SessionsActiveName = "slt.sessions.active";
    public const string WhitelistRejectionsName = "slt.whitelist.rejections";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    public static void RecordCommand(string command, string outcome, TimeSpan duration)
    {
        var tags = new TagList
        {
            { CommandTag, command },
            { OutcomeTag, outcome },
        };

        Commands.Add(1, tags);
        CommandDuration.Record(duration.TotalSeconds, tags);
    }

    public static void RecordWhitelistRejection()
    {
        WhitelistRejections.Add(1);
    }

    public static void TrackState(Func<int> observeActiveSessions)
    {
        Meter.CreateObservableGauge(
            SessionsActiveName,
            observeActiveSessions,
            "{session}",
            "Listening rooms that currently exist"
        );
    }

    private static readonly Meter Meter = new(MeterName);

    private static readonly Counter<long> Commands = Meter.CreateCounter<long>(
        CommandsCounterName,
        "{command}",
        "Commands the bot executed, by command and how they ended"
    );

    private static readonly Histogram<double> CommandDuration = Meter.CreateHistogram<double>(
        CommandDurationName,
        "s",
        "Wall clock duration of a command, from the guards in CommandBase to the last Telegram reply"
    );

    private static readonly Counter<long> WhitelistRejections = Meter.CreateCounter<long>(
        WhitelistRejectionsName,
        "{rejection}",
        "Commands refused because the user is not on the whitelist"
    );
}
