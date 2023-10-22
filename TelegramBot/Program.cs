using Core.Sessions;
using Core.Settings;
using Core.TelegramWorker;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

var builder = WebApplication.CreateBuilder(args);

var telegramSettingsSection = builder.Configuration.GetRequiredSection("Telegram");
builder.Services.Configure<TelegramSettings>(telegramSettingsSection);
var spotifySettingsSection = builder.Configuration.GetRequiredSection("Spotify");
builder.Services.Configure<SpotifySettings>(spotifySettingsSection);

builder.Services.AddSingleton<ISessionsService, SessionsService>();
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