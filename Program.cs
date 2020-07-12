//Reference: https://github.com/discord-net/Discord.Net/blob/dev/samples/02_commands_framework/Program.cs

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using DarkBot.Services;

namespace DarkBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("The bot takes one argument, the token.");
                return;
            }

            new Program().MainAsync(args[0]).GetAwaiter().GetResult();
        }

        public async Task MainAsync(string token)
        {
            BotModuleLoader bml = new BotModuleLoader();
            bml.Load();

            // You should dispose a service provider created using ASP.NET
            // when you are finished using it, at the end of your app's lifetime.
            // If you use another dependency injection framework, you should inspect
            // its documentation for the best way to do this.
            using (ServiceProvider services = ConfigureServices(bml))
            {
                DiscordSocketClient client = services.GetRequiredService<DiscordSocketClient>();

                client.Log += LogAsync;
                services.GetRequiredService<CommandService>().Log += LogAsync;

                // Tokens should be considered secret data and never hard-coded.
                // We can read from the environment variable to avoid hardcoding.
                await client.LoginAsync(TokenType.Bot, token);
                await client.StartAsync();

                // Here we initialize the logic required to register our commands.
                await services.GetRequiredService<CommandHandlingService>().InitializeAsync(bml);

                foreach (Type t in bml.GetServices())
                {
                    BotModule bm = services.GetService(t) as BotModule;
                    if (bm != null)
                    {
                        await bm.Initialize(services);
                    }
                }

                await Task.Delay(Timeout.Infinite);
            }
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        private ServiceProvider ConfigureServices(BotModuleLoader bml)
        {
            ServiceCollection sc = new ServiceCollection();
            sc.AddSingleton<DiscordSocketClient>();
            sc.AddSingleton<CommandService>();
            sc.AddSingleton<CommandHandlingService>();
            sc.AddSingleton<HttpClient>();
            bml.LoadServices(sc);
            return sc.BuildServiceProvider();
        }
    }
}