using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace DarkBot.Services
{
    public class CommandHandlingService
    {
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _discord;
        private readonly IServiceProvider _services;
        private Dictionary<ulong, char> prefixes = new Dictionary<ulong, char>();

        public CommandHandlingService(IServiceProvider services)
        {
            _commands = services.GetRequiredService<CommandService>();
            _discord = services.GetRequiredService<DiscordSocketClient>();
            _services = services;

            // Hook CommandExecuted to handle post-command-execution logic.
            _commands.CommandExecuted += CommandExecutedAsync;
            // Hook MessageReceived so we can process each message to see
            // if it qualifies as a command.
            _discord.MessageReceived += MessageReceivedAsync;
        }

        public async Task InitializeAsync(BotModuleLoader bml)
        {
            // Register modules that are public and inherit ModuleBase<T>.
            LoadPrefixes();
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            foreach (Assembly ass in bml.GetAssemblies())
            {
                await _commands.AddModulesAsync(ass, _services);
            }
        }

        private void SavePrefixes()
        {
            lock (prefixes)
            {
                StringBuilder sb = new StringBuilder();
                foreach (KeyValuePair<ulong, char> kvp in prefixes)
                {
                    sb.AppendLine($"{kvp.Key}={kvp.Value}");
                }
                DataStore.Save("Prefixes", sb.ToString());
            }
        }

        private void LoadPrefixes()
        {
            lock (prefixes)
            {
                prefixes.Clear();
                string prefixesString = DataStore.Load("Prefixes");
                if (prefixesString == null)
                {
                    return;
                }
                using (StringReader sr = new StringReader(prefixesString))
                {
                    string currentLine = null;
                    while ((currentLine = sr.ReadLine()) != null)
                    {
                        if (currentLine.Length == 0)
                        {
                            continue;
                        }
                        if (currentLine[0] == '#')
                        {
                            continue;
                        }
                        int splitIndex = currentLine.IndexOf("=");
                        if (splitIndex > 0)
                        {
                            string lhs = currentLine.Substring(0, splitIndex);
                            if (ulong.TryParse(lhs, out ulong serverID))
                            {
                                char rhs = currentLine[splitIndex + 1];
                                prefixes.Add(serverID, rhs);
                            }
                        }
                    }
                }
            }
        }

        public void ChangePrefix(ulong serverID, char prefix)
        {
            lock (prefixes)
            {
                prefixes[serverID] = prefix;
            }
            SavePrefixes();
        }

        public async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            // Ignore system messages, or messages from other bots
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;

            // This value holds the offset where the prefix ends
            var argPos = 0;

            // Perform prefix check. You may want to replace this with
            // (!message.HasCharPrefix('!', ref argPos))
            // for a more traditional command format like !help.b
            bool runCommand = message.HasMentionPrefix(_discord.CurrentUser, ref argPos);
            if (!runCommand)
            {
                if (message.Channel is SocketGuildChannel guildChannel)
                {
                    ulong guildID = guildChannel.Guild.Id;
                    if (prefixes.ContainsKey(guildID))
                    {
                        runCommand = message.HasCharPrefix(prefixes[guildID], ref argPos);
                    }
                }
                if (message.Channel is SocketDMChannel)
                {
                    runCommand = true;
                }
            }

            if (!runCommand)
            {
                return;
            }

            //Execute
            var context = new SocketCommandContext(_discord, message);
            await _commands.ExecuteAsync(context, argPos, _services);
        }

        public async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            // command is unspecified when there was a search failure (command not found); we don't care about these errors
            if (!command.IsSpecified)
                return;

            // the command was successful, we don't care about this result, unless we want to log that a command succeeded.
            if (result.IsSuccess)
                return;

            // the command failed, let's notify the user that something happened.
            await context.Channel.SendMessageAsync($"error: {result}");
        }
    }
}
