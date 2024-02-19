using Core.Commands.Factory;
using Core.Commands.Recognize;
using Core.Database;
using Core.Sessions;
using Core.Settings;
using Core.Spotify.Auth.Storage;
using Core.Spotify.Client;
using Core.Spotify.Links;
using Core.TelegramWorker;
using Microsoft.Extensions.Options;
using SqlRepositoryBase.Configuration.Extensions;
using Telegram.Bot;
using TelemetryApp.Utilities.Extensions;

var builder = WebApplication.CreateBuilder(args);

var telegramSettingsSection = builder.Configuration.GetRequiredSection("Telegram");
builder.Services.Configure<TelegramSettings>(telegramSettingsSection);
var spotifySettingsSection = builder.Configuration.GetRequiredSection("Spotify");
builder.Services.Configure<SpotifySettings>(spotifySettingsSection);

var telemetrySettingsSection = builder.Configuration.GetRequiredSection("Telemetry");
builder.Services.ConfigureTelemetryClientWithLogger("SpotifyListenTogether", "TelegramBot", telemetrySettingsSection["ApiUrl"]);

builder.Services.ConfigureConnectionStringFromAppSettings(builder.Configuration.GetSection("PostgreSql"))
       .ConfigureDbContextFactory(connectionString => new DatabaseContext(connectionString))
       .ConfigurePostgreSql();
builder.Services.AddTransient<ITokensRepository, TokensRepository>();
builder.Services.AddTransient<ISpotifyLinksRecognizeService, SpotifyLinksRecognizeService>();
builder.Services.AddSingleton<ISessionsService, SessionsService>();
builder.Services.AddSingleton<ISpotifyClientStorage, SpotifyClientStorage>();
builder.Services.AddTransient<ISpotifyClientFactory, SpotifyClientFactory>();
builder.Services.AddTransient<ICommandsRecognizer, CommandsRecognizer>();
builder.Services.AddTransient<ICommandsFactory, CommandsFactory>();
builder.Services.AddTransient<ITelegramBotWorker, TelegramBotWorker>();

builder.Services.AddSingleton<ITelegramBotClient>(
    serviceProvider =>
    {
        var telegramSettings = serviceProvider.GetRequiredService<IOptions<TelegramSettings>>();
        return new TelegramBotClient(telegramSettings.Value.BotToken);
    }
);

var app = builder.Build();
var telegramBotWorker = app.Services.GetRequiredService<ITelegramBotWorker>();
await telegramBotWorker.StartAsync();
