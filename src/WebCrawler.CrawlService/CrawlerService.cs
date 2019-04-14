// -----------------------------------------------------------------------
// <copyright file="CrawlerService.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.Threading.Tasks;
using Akka.Actor;
using WebCrawler.Shared.Config;
using WebCrawler.Shared.DevOps;

namespace WebCrawler.CrawlService
{
    public class CrawlerService
    {
        protected ActorSystem ClusterSystem;

        public Task WhenTerminated => ClusterSystem.WhenTerminated;


        public bool Start()
        {
            var config = HoconLoader.ParseConfig("crawler.hocon");
            ClusterSystem = ActorSystem.Create("webcrawler", config.ApplyOpsConfig()).StartPbm();
            return true;
        }

        public async Task Stop()
        {
            await CoordinatedShutdown.Get(ClusterSystem).Run(CoordinatedShutdown.ClrExitReason.Instance);
        }
    }
}