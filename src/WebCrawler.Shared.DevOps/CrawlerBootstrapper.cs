using System;
using Akka.Bootstrap.Docker;
using WebCrawler.Shared.DevOps.Config;

namespace WebCrawler.Shared.DevOps
{
    /// <summary>
    /// Used to help inject and standardize all of the different components
    /// needed to run all of the crawler services in production.
    /// </summary>
    public static class CrawlerBootstrapper
    {
        public static Akka.Configuration.Config ApplyOpsConfig(this Akka.Configuration.Config previousConfig)
        {
            var nextConfig = previousConfig.BootstrapFromDocker();
            return OpsConfig.GetOpsConfig().WithFallback(nextConfig);
        }
    }
}
