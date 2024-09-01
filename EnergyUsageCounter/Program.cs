using GoDota2_Bot;

namespace EnergyUsageCounter
{
    internal class Program
    {
        static void Main()
        {
            BotLogic.MainBot();
            PowerUsageMonitor.RunPowerUsageMonitor();         
        }
    }
}
