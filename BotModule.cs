using System;
using System.Threading.Tasks;

namespace DarkBot
{
    public interface BotModule
    {
        Task Initialize(IServiceProvider services);
    }
}