//Reference: https://github.com/discord-net/Discord.Net/blob/dev/samples/02_commands_framework/Program.cs

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
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

                // Here we initialize the logic required to register our commands.
                await services.GetRequiredService<CommandHandlingService>().InitializeAsync(bml);

                Queue<Type> loadModules = new Queue<Type>();
                foreach (Type t in bml.GetServices())
                {
                    loadModules.Enqueue(t);
                }

                HashSet<Type> loadedTypes = new HashSet<Type>();
                int failLoads = 0;
                while (loadModules.Count > 0)
                {
                    Type loadType = loadModules.Dequeue();
                    bool loadThisModule = true;
                    BotModuleDependency att = (BotModuleDependency)Attribute.GetCustomAttribute(loadType, typeof(BotModuleDependency));
                    if (att != null)
                    {
                        foreach (Type depType in att.dependencies)
                        {
                            if (!loadedTypes.Contains(depType))
                            {
                                loadThisModule = false;
                            }
                        }
                    }
                    if (loadThisModule)
                    {
                        failLoads = 0;
                        loadedTypes.Add(loadType);
                        BotModule bm = services.GetService(loadType) as BotModule;
                        Console.WriteLine($"Loaded {loadType.Name}");
                        await bm.Initialize(services);
                    }
                    else
                    {
                        failLoads++;
                        loadModules.Enqueue(loadType);
                        Console.WriteLine($"Delaying load for {loadType.Name}, dependency not loaded");
                    }
                    if (failLoads > loadModules.Count * 2)
                    {
                        Console.WriteLine("Not all modules loaded, please install missing dependencies");
                        break;
                    }
                }

                // Tokens should be considered secret data and never hard-coded.
                // We can read from the environment variable to avoid hardcoding.
                await client.LoginAsync(TokenType.Bot, token);
                await client.StartAsync();
                await Task.Delay(Timeout.Infinite);
            }            
        }

        public static Task LogAsync(LogMessage log)
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