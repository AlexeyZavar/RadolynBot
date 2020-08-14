#region

using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using RadLibrary.Configuration;
using RadLibrary.Configuration.Managers;
using RadLibrary.Logging;

#endregion

namespace RadBot
{
    internal class Program
    {
        private DiscordSocketClient _client;
        private AppConfiguration _config;
        private IServiceProvider _provider;
        private CommandHandler _handler;
        private CommandService _commandService;

        private static void Main(string[] args)
        {
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        private async Task MainAsync()
        {
            LogManager.AddExceptionsHandler();

            _config = AppConfiguration.Initialize<FileManager>("bot");

            await Helper.Initialize(_config);

            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose
            });

            _commandService = new CommandService();

            _client.Log += Log;

            _provider = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commandService)
                .AddSingleton(_config)
                .BuildServiceProvider();

            _handler = new CommandHandler(_client, _commandService, _provider);

            await _handler.InstallCommandsAsync();

            await _client.LoginAsync(TokenType.Bot, _config["token"]);
            await _client.SetActivityAsync(new Game("Radolyn's Maid"));
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private static Task Log(LogMessage msg)
        {
            var logger = LogManager.GetClassLogger();

            switch (msg.Severity)
            {
                case LogSeverity.Critical:
                    logger.Fatal(Helper.FormatException(msg.Exception));
                    break;
                case LogSeverity.Error:
                    logger.Error(Helper.FormatException(msg.Exception));
                    break;
                case LogSeverity.Warning:
                    logger.Warn(msg.Message);
                    break;
                case LogSeverity.Info:
                    logger.Info(msg.Message);
                    break;
                case LogSeverity.Verbose:
                    logger.Trace(msg.Message);
                    break;
                case LogSeverity.Debug:
                    logger.Debug(msg.Message);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return Task.CompletedTask;
        }
    }
}