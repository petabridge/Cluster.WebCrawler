// -----------------------------------------------------------------------
// <copyright file="CrawlerBootstrapper.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Net;
using System.Text;
using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Cluster.Hosting.SBR;
using Akka.Configuration;
using Akka.Discovery.KubernetesApi;
using Akka.HealthCheck.Hosting;
using Akka.Hosting;
using Akka.Management;
using Akka.Management.Cluster.Bootstrap;
using Microsoft.Extensions.Configuration;
using Petabridge.Cmd.Cluster;
using Petabridge.Cmd.Host;
using WebCrawler.Shared.DevOps.Config;
using ClusterOptions = WebCrawler.Shared.DevOps.Config.ClusterOptions;

namespace WebCrawler.Shared.DevOps
{
    /// <summary>
    ///     Used to help inject and standardize all of the different components
    ///     needed to run all of the crawler services in production.
    /// </summary>
    public static class CrawlerBootstrapper
    {
        public static AkkaConfigurationBuilder WithOps(
            this AkkaConfigurationBuilder builder,
            Akka.Cluster.Hosting.ClusterOptions clusterOptions,
            IConfiguration config)
        {
            // Akka.Cluster split-brain resolver configurations
            clusterOptions.SplitBrainResolver = new KeepMajorityOption();
            
            var options = GetEnvironmentVariables(config);
            
            // Clear seed nodes if we're using Config or Kubernetes Discovery
            if (options is { StartupMethod: StartupMethod.ConfigDiscovery or StartupMethod.KubernetesDiscovery })
                options.Seeds = null;
            
            builder
                .AddHoconFile("shared.hocon", HoconAddMode.Prepend)
                .AddHocon(config.GetSection("Akka"), HoconAddMode.Prepend)
                .WithClustering(clusterOptions)
                .SetupFromEnvironment(options)
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

            // No need to setup seed based cluster
            if (options is null or { StartupMethod: StartupMethod.SeedNodes })
            {
                Console.WriteLine("Forming cluster using seed nodes");
                return builder;
            }

            if (options.Discovery is null)
                throw new ConfigurationException("Cluster start up is set to discovery but discovery option is null");

            #region Config discovery setup
            if (options.StartupMethod is StartupMethod.ConfigDiscovery )
            {
                Console.WriteLine("Forming cluster using Akka.Discovery.Config");
                
                // Add Akka.Management.Cluster.Bootstrap support
                if (options.Discovery.ConfigEndpoints is null)
                    throw new ConfigurationException(
                        "Cluster start up is set to configuration discovery but discovery endpoints is null");

                builder.WithClusterBootstrap(setup =>
                {
                    setup.ContactPointDiscovery.ServiceName = options.Discovery.ServiceName;
                }, autoStart: true);
                
                var configOptions = options.Discovery;
                var sb = new StringBuilder();
                sb.AppendLine("akka.discovery.method = config");
                sb.AppendLine(
                    $"akka.discovery.config.services.{configOptions.ServiceName}.endpoints = [" +
                    $"{string.Join(",", configOptions.ConfigEndpoints?.Select(s => s.ToHocon()) ?? new[] { "" })}" +
                    "]");
                
                builder.AddHocon(sb.ToString(), HoconAddMode.Prepend);
                return builder;
            }
            #endregion

            #region Kubernetes discovery setup
            Console.WriteLine("Forming cluster using Akka.Discovery.KubernetesApi");
            
            if (options.StartupMethod is not StartupMethod.KubernetesDiscovery)
                throw new ConfigurationException($"Unknown startup method: {options.StartupMethod}");

            // Add Akka.Management.Cluster.Bootstrap support
            builder
                .WithClusterBootstrap(setup =>
                {
                    setup.ContactPointDiscovery.ServiceName = options.Discovery.ServiceName;
                    setup.ContactPointDiscovery.PortName = "management";
                }, autoStart: true)
                .WithKubernetesDiscovery();
            
            return builder;
            #endregion
        }

        private static ClusterOptions? GetEnvironmentVariables(IConfiguration configuration)
        {
            var section = configuration.GetSection("Cluster");
            if(!section.GetChildren().Any())
            {
                Console.WriteLine("Skipping environment variable bootstrap. No 'CLUSTER' section found");
                return null;
            }
            
            var options = section.Get<ClusterOptions>();
            if (options is null)
            {
                Console.WriteLine("Skipping environment variable bootstrap. Could not bind IConfiguration to 'DockerOptions'");
                return null;
            }

            return options;
        }
        
        private static AkkaConfigurationBuilder SetupFromEnvironment(
            this AkkaConfigurationBuilder builder, ClusterOptions? options)
        {
            // Skip if there are no environment setup 
            if (options is null)
                return builder;
            
            var sb = new StringBuilder();
        
            // Read CLUSTER__IP variable
            var ip = options.Ip?.Trim();
            if (!string.IsNullOrEmpty(ip))
            {
                sb.AppendLine($"akka.remote.dot-netty.tcp.public-hostname = {ip.ToHocon()}");
                Console.WriteLine($"From environment: IP: {ip}");
            }
            else
            {
                sb.AppendLine($"akka.remote.dot-netty.tcp.public-hostname = {Dns.GetHostName().ToHocon()}");
                Console.WriteLine($"From environment: IP NULL, defaulting to: {Dns.GetHostName().ToHocon()}");
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