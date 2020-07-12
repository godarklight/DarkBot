using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Reflection;

namespace DarkBot
{
    public class BotModuleLoader
    {
        private List<Assembly> assemblies = new List<Assembly>();
        private List<Type> services = new List<Type>();

        public void Load()
        {
            string pluginsPath = Path.Combine(Environment.CurrentDirectory, "Plugins");
            if (!Directory.Exists(pluginsPath))
            {
                Directory.CreateDirectory(pluginsPath);
            }
            foreach (string dllName in Directory.GetFiles(pluginsPath, "*.dll", SearchOption.AllDirectories))
            {
                try
                {
                    Console.WriteLine("Loaded module: " + dllName);
                    assemblies.Add(Assembly.LoadFile(dllName));
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error loading module: {dllName}, exception: {e.Message}");
                }
            }
        }

        public void LoadServices(IServiceCollection sc)
        {
            foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type t in ass.GetExportedTypes())
                {
                    if (t.IsClass && typeof(BotModule).IsAssignableFrom(t))
                    {
                        services.Add(t);
                        sc.AddSingleton(t);
                    }
                }
            }
        }

        public Assembly[] GetAssemblies()
        {
            return assemblies.ToArray();
        }

        public Type[] GetServices()
        {
            return services.ToArray();
        }
    }
}