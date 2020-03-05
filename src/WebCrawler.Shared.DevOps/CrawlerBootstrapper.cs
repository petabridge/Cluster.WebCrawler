// -----------------------------------------------------------------------
// <copyright file="CrawlerBootstrapper.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using Akka.Actor;
using Akka.Bootstrap.Docker;
using Hocon;
using Petabridge.Cmd.Cluster;
using Petabridge.Cmd.Host;
using WebCrawler.Shared.DevOps.Config;

namespace WebCrawler.Shared.DevOps
{
    /// <summary>
    ///     Used to help inject and standardize all of the different components
    ///     needed to run all of the crawler services in production.
    /// </summary>
    public static class CrawlerBootstrapper
    {
        public static Hocon.Config ApplyOpsConfig(this Hocon.Config previousConfig)
        {
            var nextConfig = previousConfig.BootstrapFromDocker();
            return OpsConfig.GetOpsConfig().ApplyPhobosConfig().WithFallback(nextConfig);
        }

        public static Hocon.Config ApplyPhobosConfig(this Hocon.Config previousConfig)
        {
            return OpsConfig.PhobosEnabled ? 
                OpsConfig.GetPhobosConfig().WithFallback(previousConfig) : 
                previousConfig;
        }

        /// <summary>
        ///     Start Petabridge.Cmd
        /// </summary>
        /// <param name="system">The <see cref="ActorSystem" /> that will run Petabridge.Cmd</param>
        /// <returns>The same <see cref="ActorSystem" /></returns>
        public static ActorSystem StartPbm(this ActorSystem system)
        {
            var pbm = PetabridgeCmd.Get(system);
            pbm.RegisterCommandPalette(ClusterCommands.Instance); // enable cluster management commands
            pbm.Start();
            return system;
        }
    }
}