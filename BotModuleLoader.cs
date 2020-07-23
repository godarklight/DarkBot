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
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            string pluginsPath = Path.Combine(Environment.CurrentDirectory, "Plugins");
            if (!Directory.Exists(pluginsPath))
            {
                Directory.CreateDirectory(pluginsPath);
            }
            foreach (string dllName in Directory.GetFiles(pluginsPath, "*.dll", SearchOption.AllDirectories))
            {
                try
                {
                    Console.WriteLine("Loading module: " + dllName);
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

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            //This will find and return the assembly requested if it is already loaded
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.FullName == args.Name)
                {
                    //Console.WriteLine("Resolved plugin assembly reference: " + args.Name + " (referenced by " + args.RequestingAssembly.FullName + ")");
                    return assembly;
                }
            }

            //Console.WriteLine("Could not resolve assembly " + args.Name + " referenced by " + args.RequestingAssembly.FullName);
            return null;
        }

    }
}