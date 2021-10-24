// -----------------------------------------------------------------------
// <copyright file="CrawlerBootstrapper.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using Akka.Actor;
using Akka.Bootstrap.Docker;
using Akka.Configuration;
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
                
        public static readonly Akka.Configuration.Config K8sConfig =
            ConfigurationFactory.FromResource<OpsConfig>("WebCrawler.Shared.DevOps.Config.k8sbootstrap.conf");

        public static readonly Akka.Configuration.Config AkkaManagementConfig = @"
            extensions = [""Akka.Management.Cluster.Bootstrap.ClusterBootstrapProvider, Akka.Management.Cluster.Bootstrap""]
        ";
        
        public static Akka.Configuration.Config ApplyOpsConfig(this Akka.Configuration.Config previousConfig)
        {
            var nextConfig = AkkaManagementConfig.WithFallback(K8sConfig).WithFallback(previousConfig.BootstrapFromDocker());
            return OpsConfig.GetOpsConfig().ApplyPhobosConfig().WithFallback(nextConfig);
        }

        public static Akka.Configuration.Config ApplyPhobosConfig(this Akka.Configuration.Config previousConfig)
        {
            var enabledPhobosStr =
                Environment.GetEnvironmentVariable(OpsConfig.PHOBOS_ENABLED)?.Trim().ToLowerInvariant() ?? "false";
            if (bool.TryParse(enabledPhobosStr, out var enabledPhobos) && enabledPhobos)
                return OpsConfig.GetPhobosConfig().WithFallback(previousConfig);

            return previousConfig;
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