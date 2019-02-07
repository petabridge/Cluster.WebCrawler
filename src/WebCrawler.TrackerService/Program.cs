// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Petabridge, LLC">
//      Copyright (C) 2018 - 2018 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace WebCrawler.TrackerService
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var trackingService = new TrackerService();
            trackingService.Start();

            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
            {
                trackingService.Stop().Wait(TimeSpan.FromSeconds(30));
            };

            Console.CancelKeyPress += async (sender, eventArgs) =>
            {
                await trackingService.Stop();
                eventArgs.Cancel = true;
            };

            trackingService.WhenTerminated.Wait();
        }
    }
}