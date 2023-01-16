// -----------------------------------------------------------------------
// <copyright file="CrawlerBootstrapper.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Net;
using System.Text;
using Akka.Cluster.Hosting;
using Akka.Cluster.Hosting.SBR;
using Akka.HealthCheck.Hosting;
using Akka.Hosting;
using Microsoft.Extensions.Configuration;
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
        public static AkkaConfigurationBuilder WithOps(this AkkaConfigurationBuilder builder, ClusterOptions clusterOptions, IConfiguration config)
        {
            // Akka.Cluster split-brain resolver configurations
            clusterOptions.SplitBrainResolver = new KeepMajorityOption();
            
            builder
                .AddHoconFile("shared.hocon", HoconAddMode.Prepend)
                .AddHocon(config.GetSection("Akka"), HoconAddMode.Prepend)
                .WithClustering(clusterOptions)
                .BootstrapFromDocker(config)
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

        private static AkkaConfigurationBuilder BootstrapFromDocker(this AkkaConfigurationBuilder builder, IConfiguration configuration)
        {
            var section = configuration.GetSection("Cluster");
            if(!section.GetChildren().Any())
                Console.WriteLine("Skipping environment variable bootstrap. No 'CLUSTER' section found");
            
            var options = section.Get<DockerOptions>();
            if (options is null)
            {
                Console.WriteLine("Skipping environment variable bootstrap. Could not bind IConfiguration to 'DockerOptions'");
                return builder;
            }
            
            var sb = new StringBuilder();
        
            // Read CLUSTER__IP variable
            var ip = options.Ip?.Trim();
            if (!string.IsNullOrEmpty(ip))
            {
                sb.AppendLine($"akka.remote.dot-netty.tcp.public-hostname = {ip}");
                Console.WriteLine($"From environment: IP: {ip}");
            }
            else
            {
                sb.AppendLine($"akka.remote.dot-netty.tcp.public-hostname = {Dns.GetHostName()}");
                Console.WriteLine($"From environment: IP NULL, defaulting to: {Dns.GetHostName()}");
            }
        
            // Read CLUSTER__PORT variable
            if (options.Port is { })
            {
                sb.AppendLine($"akka.remote.dot-netty.tcp.port = {options.Port}");
                Console.WriteLine($"From environment: PORT: {options.Port}");
            }
            else
            {
                Console.WriteLine("From environment: PORT: NULL");
            }

            // Read CLUSTER__SEEDS variable
            if(options.Seeds is { })
            {
                var seeds = string.Join(",", options.Seeds.Select(s => s.ToHocon()));
                sb.AppendLine(
                    $"akka.cluster.seed-nodes = [{seeds}]");
                Console.WriteLine($"From environment: SEEDS: [{seeds}]");
            }
            else
            {
                Console.WriteLine("From environment: SEEDS: []");
            }

            if (sb.Length > 0)
                builder.AddHocon(sb.ToString(), HoconAddMode.Prepend);

            return builder;
        }
    }
}