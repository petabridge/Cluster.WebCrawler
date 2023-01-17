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
using Akka.Remote.Hosting;
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
            RemoteOptions remoteOptions,
            Akka.Cluster.Hosting.ClusterOptions clusterOptions,
            IConfiguration config)
        {
            // Akka.Cluster split-brain resolver configurations
            clusterOptions.SplitBrainResolver = new KeepMajorityOption();
            
            var options = GetEnvironmentVariables(config);
            
            // Clear seed nodes if we're using Config or Kubernetes Discovery
            if (options is { StartupMethod: StartupMethod.ConfigDiscovery or StartupMethod.KubernetesDiscovery })
            {
                clusterOptions.SeedNodes = null;
                options.Seeds = null;
            }
            
            // Setup remoting
            // Reads environment variable CLUSTER__PORT
            if (options is { Port: { } })
            {
                Console.WriteLine($"From environment: PORT: {options.Port}");
                remoteOptions.Port = options.Port;
            }
            else
            {
                Console.WriteLine($"From environment: PORT: NULL. Using tcp port: {remoteOptions.Port}");
            }

            // Reads environment variable CLUSTER__IP
            if (options is { Ip: { } })
            {
                var ip = options.Ip.Trim();
                remoteOptions.PublicHostName = ip;
                Console.WriteLine($"From environment: IP: {ip}");
            }
            else
            {
                Console.WriteLine($"From environment: IP NULL, defaulting to: {Dns.GetHostName().ToHocon()}");
                remoteOptions.PublicHostName = Dns.GetHostName().ToHocon();
            }

            if (options is { Seeds: { } })
            {
                var seeds = string.Join(",", options.Seeds.Select(s => s.ToHocon()));
                clusterOptions.SeedNodes = options.Seeds;
                Console.WriteLine($"From environment: SEEDS: [{seeds}]");
            }
            else
            {
                Console.WriteLine($"From environment: SEEDS: NULL, using seeds: [{string.Join(", ", clusterOptions.SeedNodes ?? new []{ "" })}]");
            }

            
            builder
                .AddHoconFile("shared.hocon", HoconAddMode.Prepend)
                .AddHocon(config.GetSection("Akka"), HoconAddMode.Prepend)
                .WithRemoting(remoteOptions)
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
                Console.WriteLine("From environment: Forming cluster using seed nodes");
                return builder;
            }

            if (options.Discovery is null)
                throw new ConfigurationException("Cluster start up is set to discovery but discovery option is null");

            #region Config discovery setup
            if (options.StartupMethod is StartupMethod.ConfigDiscovery )
            {
                Console.WriteLine("From environment: Forming cluster using Akka.Discovery.Config");
                
                if (options.Discovery.ConfigEndpoints is null)
                    throw new ConfigurationException(
                        "Cluster start up is set to configuration discovery but discovery endpoints is null");

                builder.WithAkkaManagement(setup =>
                {
                    setup.Http.HostName = options.Ip ?? Dns.GetHostName();
                    setup.Http.Port = 8558;
                    setup.Http.BindHostName = "0.0.0.0";
                    setup.Http.BindPort = 8558;
                });
                
                // Add Akka.Management.Cluster.Bootstrap support
                builder.WithClusterBootstrap(setup =>
                {
                    setup.ContactPointDiscovery.ServiceName = options.Discovery.ServiceName;
                }, autoStart: true);
                
                var configOptions = options.Discovery;
                var endpoints = string.Join(",", configOptions.ConfigEndpoints.Select(s => s.ToHocon()));
                Console.WriteLine($"From environment: Using config based discovery endpoints: [{endpoints}]");
                
                var sb = new StringBuilder();
                sb.AppendLine("akka.discovery.method = config");
                sb.AppendLine("akka.discovery.config {");
                sb.AppendLine("class = \"Akka.Discovery.Config.ConfigServiceDiscovery, Akka.Discovery\"");
                sb.AppendLine("services-path = \"akka.discovery.config.services\"");
                sb.AppendLine($"services.{configOptions.ServiceName}.endpoints = [{endpoints}]");
                sb.AppendLine("}");
                
                builder.AddHocon(sb.ToString(), HoconAddMode.Prepend);
                return builder;
            }
            #endregion

            #region Kubernetes discovery setup
            Console.WriteLine("From environment: Forming cluster using Akka.Discovery.KubernetesApi");
            
            if (options.StartupMethod is not StartupMethod.KubernetesDiscovery)
                throw new ConfigurationException($"From environment: Unknown startup method: {options.StartupMethod}");

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
            }

            if (sb.Length > 0)
                builder.AddHocon(sb.ToString(), HoconAddMode.Prepend);

            return builder;
        }
    }
}