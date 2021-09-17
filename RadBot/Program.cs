#region

using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using RadLibrary.Configuration;
using RadLibrary.Configuration.Managers;
using Serilog;

#endregion

namespace RadBot
{
    internal static class Program
    {
        private static DiscordSocketClient _client;
        private static CommandService _commandService;
        private static AppConfiguration _config;
        private static CommandHandler _handler;
        private static IServiceProvider _provider;

        private static async Task Main()
        {
            var logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("bot.log")
                .CreateLogger();
            Log.Logger = logger;

            _config = AppConfiguration.Initialize<FileManager>("bot");

            Helper.Initialize(_config);

            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose
            });

            _commandService = new CommandService();

            _client.Log += Logging.LogClient;

            _provider = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commandService)
                .AddSingleton(_config)
                .BuildServiceProvider();

            _handler = new CommandHandler(_client, _commandService, _provider);

            await _handler.AddCommands();

            await _client.LoginAsync(TokenType.Bot, _config["token"]);
            await _client.SetActivityAsync(new Game("cute maid"));
            await _client.StartAsync();

            await Task.Delay(-1);
        }
    }
}
