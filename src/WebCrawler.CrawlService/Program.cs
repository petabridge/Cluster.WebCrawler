// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace WebCrawler.CrawlService
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var crawlerService = new CrawlerService();
            crawlerService.Start();

            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
            {
                crawlerService.Stop().Wait(TimeSpan.FromSeconds(30));
            };

            Console.CancelKeyPress += async (sender, eventArgs) =>
            {
                await crawlerService.Stop();
                eventArgs.Cancel = true;
            };

            crawlerService.WhenTerminated.Wait();
        }
    }
}