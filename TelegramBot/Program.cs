using Core.Commands.Base;
using Core.Commands.Factory;
using Core.Commands.Recognize;
using Core.Database;
using Core.Sessions;
using Core.Sessions.Storage;
using Core.Settings;
using Core.Spotify.Auth.Storage;
using Core.Spotify.Client;
using Core.Spotify.Links;
using Core.TelegramWorker;
using Core.Whitelist;
using Microsoft.Extensions.Options;
using Serilog;
using SqlRepositoryBase.Configuration.Extensions;
using Telegram.Bot;
using TelemetryApp.Utilities.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog(
    (context, configuration) => configuration.ReadFrom.Configuration(context.Configuration)
);

var telegramSettingsSection = builder.Configuration.GetRequiredSection("Telegram");
builder.Services.Configure<TelegramSettings>(telegramSettingsSection);
var spotifySettingsSection = builder.Configuration.GetRequiredSection("Spotify");
builder.Services.Configure<SpotifySettings>(spotifySettingsSection);

builder.Services.ConfigureConnectionStringFromAppSettings(builder.Configuration.GetSection("PostgreSql"))
       .ConfigureDbContextFactory(connectionString => new DatabaseContext(connectionString))
       .ConfigurePostgreSql();
builder.Services.AddTransient<ITokensRepository, TokensRepository>();
builder.Services.AddTransient<ISessionsRepository, SessionsRepository>();
builder.Services.AddTransient<ISpotifyLinksRecognizeService, SpotifyLinksRecognizeService>();
builder.Services.AddSingleton<ISessionsService, SessionsService>();
builder.Services.AddSingleton<ISpotifyClientStorage, SpotifyClientStorage>();
builder.Services.AddTransient<ISpotifyClientFactory, SpotifyClientFactory>();

builder.Services.AddTransient<ICommandsRecognizer, CommandsRecognizer>();
var allTypes = typeof(ICommandBase).Assembly.GetTypes().ToArray();
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

var sessionsService = app.Services.GetRequiredService<ISessionsService>();
await sessionsService.InitializeAsync();

var spotifyClientFactory = app.Services.GetRequiredService<ISpotifyClientFactory>();
await spotifyClientFactory.InitializeAllSavedClientsAsync();

var telegramBotWorker = app.Services.GetRequiredService<ITelegramBotWorker>();
await telegramBotWorker.StartAsync();