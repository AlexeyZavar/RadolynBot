#region

using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RadLibrary.Logging;

#endregion

namespace RadBot
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IServiceProvider _provider;

        // Retrieve client and CommandService instance via ctor
        public CommandHandler(DiscordSocketClient client, CommandService commands, IServiceProvider provider)
        {
            _commands = commands;
            _client = client;
            _provider = provider;
        }

        public async Task InstallCommandsAsync()
        {
            // Hook the MessageReceived event into our command handler
            _client.MessageReceived += HandleCommandAsync;

            _commands.CommandExecuted += LogCommand;

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(),
                _provider);
        }

        private static Task LogCommand(Optional<CommandInfo> cmd, ICommandContext context, IResult result)
        {
            var logger = LogManager.GetMethodLogger();
            logger.Info("User {0} issued {1} command. Result: {2} ({3})", context.User.Username, cmd.Value.Name,
                result.IsSuccess, result.ErrorReason ?? "Ok");

            return Task.CompletedTask;
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            // Don't process the command if it was a system message
            if (!(messageParam is SocketUserMessage message)) return;

            // Create a number to track where the prefix ends and the command begins
            var argPos = 0;

            // Determine if the message is a command based on the prefix and make sure no bots trigger commands
            if (!message.HasCharPrefix('>', ref argPos) && !message.HasMentionPrefix(_client.CurrentUser, ref argPos))
                return;

            // Create a WebSocket-based command context based on the message
            var context = new SocketCommandContext(_client, message);

            // Execute the command with the command context we just
            // created, along with the service provider for precondition checks.
            var result = await _commands.ExecuteAsync(
                context,
                argPos,
                _provider);

            if (!result.IsSuccess)
                switch (result.Error)
                {
                    case CommandError.UnknownCommand:
                        await context.Channel.SendMessageAsync(
                            "Unknown command! Type '>help' to get all available commands.");
                        break;
                    case CommandError.ParseFailed:
                    case CommandError.BadArgCount:
                    case CommandError.MultipleMatches:
                    case CommandError.ObjectNotFound:
                        await context.Channel.SendMessageAsync(
                            "Failed to parse parameters! Type '>help COMMAND' to get help.");
                        break;
                    case CommandError.UnmetPrecondition:
                        await context.Channel.SendMessageAsync("Not enough permissions.");
                        break;
                    case CommandError.Unsuccessful:
                    case CommandError.Exception:
                        await context.Channel.SendMessageAsync(
                            "Failed to execute command. Message: " + result.ErrorReason);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
        }
    }
}