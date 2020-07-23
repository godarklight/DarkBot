using System;
using System.Threading.Tasks;

namespace DarkBot
{
    public class BotModuleDependency : Attribute
    {
        public Type[] dependencies;
        public BotModuleDependency(Type[] dependencies)
        {
            this.dependencies = dependencies;
        }
    }
}