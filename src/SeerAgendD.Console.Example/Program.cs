using System;
using System.Threading;
using System.Threading.Tasks;

namespace SeerAgendD.Console.Example
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var isService = args.Length > 0 && args[0] == "--mode=service";
            
            System.Console.WriteLine($"Starting example app in {(isService ? "service" : "interactive")} mode");
            
            var counter = 0;
            while (true)
            {
                System.Console.WriteLine($"Counter: {counter++}");
                await Task.Delay(1000);
            }
        }
    }
}
