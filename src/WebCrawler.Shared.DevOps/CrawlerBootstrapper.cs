// -----------------------------------------------------------------------
// <copyright file="CrawlerBootstrapper.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Cluster.Hosting;
using Akka.Cluster.Hosting.SBR;
using Akka.Configuration;
using Akka.HealthCheck.Hosting;
using Akka.Hosting;
using Microsoft.Extensions.Configuration;
using Petabridge.Cmd.Cluster;
using Petabridge.Cmd.Host;

namespace WebCrawler.Shared.DevOps
{
    /// <summary>
    ///     Used to help inject and standardize all of the different components
    ///     needed to run all of the crawler services in production.
    /// </summary>
    public static class CrawlerBootstrapper
    {
        /// <summary>
        ///     Name of the config property used to look for Phobos
        /// </summary>
        public const string PhobosEnabled = "phobos-enabled";

        /// <summary>
        ///     Name of the config property used to direct Phobos' StatsD
        ///     output.
        /// </summary>
        public const string StatsdUrl = "statsd-url";

        /// <summary>
        ///     Name of the config property used to direct Phobos' StatsD
        ///     output.
        /// </summary>
        public const string StatsdPort = "statsd-port";
        
        
        private static readonly string PetabridgeCmdHocon = @"
# See petabridge.cmd configuration options here: https://cmd.petabridge.com/articles/install/host-configuration.html
petabridge.cmd{
	# default IP address used to listen for incoming petabridge.cmd client connections
	# should be a safe default as it listens on 'all network interfaces'.
    host = ""0.0.0.0""

    # default port number used to listen for incoming petabridge.cmd client connections
    port = 9110
}";
        
        public static ClusterOptions WithOps(this ClusterOptions options)
        {
            // Akka.Cluster split-brain resolver configurations
            options.SplitBrainResolver = new KeepMajorityOption();
            
            return options;
        }

        public static AkkaConfigurationBuilder WithOps(this AkkaConfigurationBuilder builder, IConfiguration config)
        {
            builder
                .AddHocon(config, HoconAddMode.Prepend)
                .BootstrapFromDocker()
                .AddHocon(PetabridgeCmdHocon, HoconAddMode.Prepend)
                .AddPetabridgeCmd(pbm =>
                {
                    // enable cluster management commands
                    pbm.RegisterCommandPalette(ClusterCommands.Instance); 
                })
                // Not explicitly setting the liveness provider. The Akka.Remote port
                // is usually an effective-enough tool for this.
                .WithHealthCheck(opt =>
                {
                    // Use a second socket for TCP readiness checks from K8s
                    opt.Readiness.Transport = HealthCheckTransport.Tcp;
                    opt.Readiness.TcpPort = 11001;
                });

            return builder;
        }

        public static AkkaConfigurationBuilder WithPhobos(this AkkaConfigurationBuilder builder)
        {
            if (!builder.Configuration.HasValue)
                return builder;

            var config = builder.Configuration.Value;
            if (!config.HasPath(PhobosEnabled) || !config.GetBoolean(PhobosEnabled))
                return builder;
            
            builder.AddHocon(
                ConfigurationFactory.FromResource<MarkerClass>("WebCrawler.Shared.DevOps.Config.crawler.Phobos.conf"), 
                HoconAddMode.Prepend);


            var statsdUrl = config.GetString(StatsdUrl);
            var statsDPort = config.GetString(StatsdPort);
            if (!string.IsNullOrEmpty(statsdUrl) && int.TryParse(statsDPort, out var portNum))
            {
                builder.AddHocon(@$"
phobos.monitoring.statsd.endpoint = ""{statsdUrl}""
phobos.monitoring.statsd.port={portNum}", HoconAddMode.Prepend);
            }

            return builder;
        }
    }
}