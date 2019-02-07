using System;
using System.Diagnostics;

namespace Lighthouse
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var lighthouseService = new LighthouseService();
            lighthouseService.Start();
            Console.WriteLine("Press Control + C to terminate.");

            AppDomain.CurrentDomain.ProcessExit += async (sender, eventArgs) =>
            {
                await lighthouseService.StopAsync();
            };

            Console.CancelKeyPress += async (sender, eventArgs) =>
            {
                await lighthouseService.StopAsync();
            };
            lighthouseService.TerminationHandle.Wait(); 
        }
    }
}
