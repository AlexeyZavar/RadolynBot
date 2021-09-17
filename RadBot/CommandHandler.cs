#region

using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using RadLibrary.Configuration;

#endregion

namespace RadBot
{
    public sealed class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly AppConfiguration _config;
        private readonly IServiceProvider _services;

        public CommandHandler(DiscordSocketClient client, CommandService commands, IServiceProvider services)
        {
            _client = client;
            _commands = commands;
            _services = services;

            _config = _services.GetService(typeof(AppConfiguration)) as AppConfiguration;
        }

        public async Task AddCommands()
        {
            _client.MessageReceived += HandleCommandAsync;
            _commands.CommandExecuted += Logging.LogCommandService;

            await _commands.AddModulesAsync(typeof(Config).Assembly,
                _services);
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            if (messageParam is not SocketUserMessage message || message.Author.IsBot) return;

            var argPos = 0;

            if (!message.HasStringPrefix(_config["prefix"], ref argPos))
                return;

            var context = new SocketCommandContext(_client, message);

            var scope = _services.CreateScope();
            var result = await _commands.ExecuteAsync(
                context,
                argPos,
                scope.ServiceProvider);

            if (!result.IsSuccess)
                switch (result.Error)
                {
                    case CommandError.UnknownCommand:
                        await context.Channel.SendMessageAsync(
                            $"What do you mean?! `{_config["prefix"]}help` if you don't know how to *use* me...");
                        break;
                    case CommandError.ParseFailed:
                    case CommandError.BadArgCount:
                    case CommandError.ObjectNotFound:
                    case CommandError.MultipleMatches:
                        await context.Channel.SendMessageAsync(
                            $"I think that you don't know how to use this command properly... `{_config["prefix"]}help COMMAND` to see how to *use* this command.");
                        break;
                    case CommandError.UnmetPrecondition:
                        await context.Channel.SendMessageAsync("I think you don't have enough permissions!");
                        break;
                    case CommandError.Exception:
                        // handled in LogCommand
                        break;
                    case CommandError.Unsuccessful:
                        // handled in LogCommand
                        break;
                    case null:
                        // ???
                        break;
                }
        }
    }
}
