#region

using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Serilog;
using Serilog.Events;

#endregion

namespace RadBot
{
    public static class Logging
    {
        public static Task LogClient(LogMessage message)
        {
            var level = message.Severity switch
            {
                LogSeverity.Critical => LogEventLevel.Fatal,
                LogSeverity.Error => LogEventLevel.Error,
                LogSeverity.Warning => LogEventLevel.Warning,
                LogSeverity.Info => LogEventLevel.Information,
                LogSeverity.Verbose => LogEventLevel.Verbose,
                LogSeverity.Debug => LogEventLevel.Debug,
                _ => throw new ArgumentOutOfRangeException(nameof(message))
            };

            Log.Write(level, message.Exception, message.Message);
            return Task.CompletedTask;
        }

        public static Task LogCommandService(Optional<CommandInfo> cmd, ICommandContext ctx, IResult result)
        {
            var commandInfo = cmd.GetValueOrDefault();

            var alias = "<unknown>";
            if (commandInfo is not null)
            {
                var module = commandInfo.Module.Aliases[0];
                if (!string.IsNullOrEmpty(module))
                    module += " ";

                alias = $"{module}{commandInfo.Name}";
            }

            Log.Information("User {User} issued {Command}. Result: {Success} ({ErrorReason})", ctx.User, alias,
                result.IsSuccess, result.ErrorReason);
            return Task.CompletedTask;
        }
    }
}
