#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using RadLibrary;
using RadLibrary.Configuration;

#endregion

namespace RadBot.Modules
{
    [Name("Random")]
    public class HelpModule : ModuleBase<SocketCommandContext>
    {
        private readonly CommandService _service;
        private readonly AppConfiguration _config;
        private readonly IServiceProvider _provider;

        public HelpModule(CommandService service, AppConfiguration config, IServiceProvider provider)
        {
            _service = service;
            _config = config;
            _provider = provider;
        }

        [Command("help")]
        [Summary("Prints help message.")]
        public async Task HelpAsync()
        {
            var builder = Helper.GetBuilder();

            var dict = new Dictionary<string, string>();
            var fields = new List<EmbedFieldBuilder>();

            foreach (var module in _service.Modules)
            {
                var localName = module.IsSubmodule ? module.Parent.Name + "." + module.Name : module.Name;

                if (!dict.ContainsKey(localName))
                    dict.Add(localName, "");

                foreach (var command in module.Commands)
                {
                    var res = await command.CheckPreconditionsAsync(Context, _provider);

                    if (!res.IsSuccess)
                        continue;

                    var cmd = _config["bulletSymbol"] + " " +
                              command.Aliases.Aggregate((current, item) =>
                                  current + ", " + Environment.NewLine + _config["bulletSymbol"].Repeat(2) + " " +
                                  item) + Environment.NewLine.Repeat(2);

                    if (!dict[localName].Contains(cmd))
                        dict[localName] += cmd;
                }
            }

            foreach (var (module, s) in dict)
            {
                if (dict[module] == "")
                    continue;

                fields.Add(new EmbedFieldBuilder
                {
                    Name = module,
                    Value = s,
                    IsInline = true
                });
            }

            builder.WithTitle("Available commands (for you)")
                .WithFields(fields);

            await ReplyAsync(embed: builder.Build());
        }

        [Command("help")]
        [Summary("Prints help message about specified command.")]
        public async Task HelpAsync([Remainder] [Summary("The command")] string command)
        {
            var found = _service.Search(command);

            if (!found.IsSuccess)
            {
                await ReplyAsync($"Can't find '{command}' command");
                return;
            }

            foreach (var cmd in found.Commands)
            {
                var builder = Helper.GetBuilder();

                var fields = new List<EmbedFieldBuilder>();

                fields.Add(new EmbedFieldBuilder
                {
                    Name = "Description:",
                    Value = cmd.Command.Summary,
                    IsInline = true
                });

                var parameters = cmd.Command.Parameters.Aggregate("",
                    (current, parameter) => current + _config["bulletSymbol"] + " " + parameter.Name + ": " +
                                            parameter.Summary + " " +
                                            (parameter.IsOptional ? "(optional)" : "(required)") +
                                            (parameter.IsRemainder ? " (remainder)" : "") + Environment.NewLine);
                var perms = GetAllPermissions(cmd.Command).Aggregate("",
                    (current, item) => _config["bulletSymbol"] + " " + item.GuildPermission);

                fields.Add(new EmbedFieldBuilder
                {
                    Name = "Parameters:",
                    Value = parameters == "" ? _config["bulletSymbol"] + " no" : parameters,
                    IsInline = true
                });

                fields.Add(new EmbedFieldBuilder
                {
                    Name = "Permissions:",
                    Value = perms == "" ? _config["bulletSymbol"] + " no" : perms,
                    IsInline = true
                });

                fields.Add(new EmbedFieldBuilder
                {
                    Name = "Module:",
                    Value = GetModuleName(cmd.Command.Module),
                    IsInline = true
                });

                builder.WithTitle($"Help for '{found.Text}' command")
                    .WithFields(fields);

                await ReplyAsync(embed: builder.Build());
            }
        }

        private static IEnumerable<RequireUserPermissionAttribute> GetAllPermissions(CommandInfo cmd)
        {
            var all = cmd.Preconditions.Where(x => x is RequireUserPermissionAttribute)
                .Cast<RequireUserPermissionAttribute>();

            return all.Concat(RecursivePermissions(cmd.Module)).ToHashSet();
        }

        private static IEnumerable<RequireUserPermissionAttribute> RecursivePermissions(ModuleInfo module)
        {
            var all = module.Preconditions.Where(x => x is RequireUserPermissionAttribute)
                .Cast<RequireUserPermissionAttribute>();

            return module.Parent != null ? all.Concat(RecursivePermissions(module.Parent)) : all;
        }

        private static string GetModuleName(ModuleInfo module)
        {
            return module.Parent == null ? module.Name : GetModuleName(module.Parent) + "." + module.Name;
        }
    }
}