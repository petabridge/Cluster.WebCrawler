using System;

namespace WebCrawler.CrawlService
{
    class Program
    {
        static void Main(string[] args)
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
