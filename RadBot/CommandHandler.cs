#region

using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RadLibrary.Configuration;
using RadLibrary.Logging;

#endregion

namespace RadBot
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly AppConfiguration _config;
        private readonly IServiceProvider _provider;

        // Retrieve client and CommandService instance via ctor
        public CommandHandler(DiscordSocketClient client, CommandService commands, IServiceProvider provider)
        {
            _commands = commands;
            _client = client;
            _provider = provider;
            _config = _provider.GetService(typeof(AppConfiguration)) as AppConfiguration;
        }

        public async Task InstallCommandsAsync()
        {
            // Hook the MessageReceived event into our command handler
            _client.MessageReceived += HandleCommandAsync;

            _commands.CommandExecuted += LogCommand;

            await _commands.AddModulesAsync(typeof(Config).Assembly,
                _provider);
        }

        private static async Task LogCommand(Optional<CommandInfo> cmd, ICommandContext context, IResult result)
        {
            string command;

            if (!cmd.IsSpecified)
            {
                command = "<unknown>";
            }
            else
            {
                var module = cmd.Value?.Module?.Aliases?[0];
                command = !string.IsNullOrEmpty(module) ? module + " " + cmd.Value?.Name : cmd.Value?.Name;
            }

            var logger = LogManager.GetClassLogger();
            logger.Info("User {0} issued \"{1}\" command. Result: {2} ({3})", context.User.Username, command,
                result.IsSuccess, result.ErrorReason);

            var res = result as ExecuteResult?;

            if (res?.IsSuccess == true || cmd.GetValueOrDefault() == null || string.IsNullOrEmpty(res?.ErrorReason))
                return;

            await context.Channel.SendMessageAsync("Failed to execute command. Exception: ```yml" +
                                                   Environment.NewLine + Helper.FormatException(res?.Exception) +
                                                   "```<@305414308320247818>");
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            // Don't process the command if it was a system message
            if (!(messageParam is SocketUserMessage message)) return;

            // Create a number to track where the prefix ends and the command begins
            var argPos = 0;

            // Determine if the message is a command based on the prefix and make sure no bots trigger commands
            if (!message.HasStringPrefix(_config["prefix"], ref argPos) &&
                !message.HasMentionPrefix(_client.CurrentUser, ref argPos) || message.Author.IsBot)
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
                }
        }
    }
}