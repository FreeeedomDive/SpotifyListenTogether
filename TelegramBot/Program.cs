using Core.Commands.Base;
using Core.Commands.Factory;
using Core.Commands.Recognize;
using Core.Database;
using Core.Sessions;
using Core.Sessions.Storage;
using Core.Settings;
using Core.Spotify.Auth.Storage;
using Core.Spotify.Links;
using Core.TelegramWorker;
using Core.Whitelist;
using Microsoft.Extensions.Options;
using Serilog;
using SpotifyHelpers.Api.Client;
using SpotifyHelpers.Api.Client.Configuration;
using SqlRepositoryBase.Configuration.Extensions;
using Telegram.Bot;
using TelegramBot.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog(
    (context, configuration) =>
    {
        configuration.ReadFrom.Configuration(context.Configuration);

        if (OpenTelemetryExtensions.IsExportEnabled())
        {
            configuration.WriteTo.WriteToOpenTelemetry();
        }
    }
);

builder.Services.AddSltTelemetry();

builder.Services.Configure<TelegramSettings>(builder.Configuration.GetRequiredSection("Telegram"));

builder.Services.Configure<SpotifyAuthApiConnectionOptions>(builder.Configuration.GetRequiredSection("SpotifyAuthApi"));
builder.Services.AddSingleton<ISpotifyHelpersApiClient>(
    serviceProvider => SpotifyHelpersApiClientProvider.Build(serviceProvider.GetRequiredService<IOptions<SpotifyAuthApiConnectionOptions>>().Value.ServiceUrl)
);

builder.Services.ConfigureConnectionStringFromAppSettings(builder.Configuration.GetSection("PostgreSql"))
       .ConfigureDbContextFactory(connectionString => new DatabaseContext(connectionString))
       .ConfigurePostgreSql();
builder.Services.AddTransient<ISpotifyProfilesService, SpotifyProfilesService>();
builder.Services.AddTransient<ISessionsRepository, SessionsRepository>();
builder.Services.AddTransient<ISpotifyLinksRecognizeService, SpotifyLinksRecognizeService>();
builder.Services.AddSingleton<ISessionsService, SessionsService>();

builder.Services.AddTransient<ICommandsRecognizer, CommandsRecognizer>();
var allTypes = typeof(ICommandBase).Assembly.GetTypes();
var commandTypes = allTypes.Where(t => typeof(ICommandBase).IsAssignableFrom(t) && t.IsInterface && t != typeof(ICommandBase)).ToArray();
foreach (var commandInterfaceType in commandTypes)
{
    var commandImplementationType = allTypes.First(t => commandInterfaceType.IsAssignableFrom(t) && !t.IsInterface);
    builder.Services.AddTransient(commandInterfaceType, commandImplementationType);
}

builder.Services.AddTransient<ICommandsFactory, CommandsFactory>();

builder.Services.AddTransient<IWhitelistService, WhitelistService>();
builder.Services.AddTransient<ITelegramBotWorker, TelegramBotWorker>();

builder.Services.AddSingleton<ITelegramBotClient>(
    serviceProvider =>
    {
        var telegramSettings = serviceProvider.GetRequiredService<IOptions<TelegramSettings>>();
        return new TelegramBotClient(telegramSettings.Value.BotToken);
    }
);

var app = builder.Build();
app.Services.StartSltTelemetry();

var sessionsService = app.Services.GetRequiredService<ISessionsService>();
await sessionsService.InitializeAsync();

var telegramBotWorker = app.Services.GetRequiredService<ITelegramBotWorker>();
await telegramBotWorker.StartAsync();
