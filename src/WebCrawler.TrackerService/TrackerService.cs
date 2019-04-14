// -----------------------------------------------------------------------
// <copyright file="TrackerService.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.Threading.Tasks;
using Akka.Actor;
using WebCrawler.Shared.Config;
using WebCrawler.Shared.DevOps;
using WebCrawler.TrackerService.Actors;
using WebCrawler.TrackerService.Actors.Tracking;

namespace WebCrawler.TrackerService
{
    public class TrackerService
    {
        protected IActorRef ApiMaster;
        protected ActorSystem ClusterSystem;
        protected IActorRef DownloadMaster;

        public Task WhenTerminated => ClusterSystem.WhenTerminated;


        public bool Start()
        {
            var config = HoconLoader.ParseConfig("tracker.hocon");
            ClusterSystem = ActorSystem.Create("webcrawler", config.ApplyOpsConfig()).StartPbm();
            ApiMaster = ClusterSystem.ActorOf(Props.Create(() => new ApiMaster()), "api");
            DownloadMaster = ClusterSystem.ActorOf(Props.Create(() => new DownloadsMaster()), "downloads");
            return true;
        }

        public async Task Stop()
        {
            await CoordinatedShutdown.Get(ClusterSystem).Run(CoordinatedShutdown.ClrExitReason.Instance);
        }
    }
}