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
using Akka.Discovery.Azure;
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
            IConfiguration config,
            int readinessPort,
            int pbmPort)
        {
            // Akka.Cluster split-brain resolver configurations
            clusterOptions.SplitBrainResolver = new KeepMajorityOption();

            #region Environment variable setup

            var options = GetEnvironmentVariables(config);
            // Clear seed nodes if we're using Config or Kubernetes Discovery
            if (options.StartupMethod is StartupMethod.ConfigDiscovery or StartupMethod.KubernetesDiscovery or StartupMethod.AzureDiscovery)
            {
                clusterOptions.SeedNodes = null;
                options.Seeds = null;
            }
            
            // Setup remoting
            // Reads environment variable CLUSTER__PORT
            if (options.Port is not null)
            {
                Console.WriteLine($"From environment: PORT: {options.Port}");
                remoteOptions.Port = options.Port;
            }
            else
            {
                Console.WriteLine($"From environment: PORT: NULL. Using tcp port: {remoteOptions.Port}");
            }

            // Reads environment variable CLUSTER__IP
            if (options.Ip is not null)
            {
                var ip = options.Ip.Trim();
                remoteOptions.PublicHostName = ip;
                Console.WriteLine($"From environment: IP: {ip}");
            }
            else if (options.IsDocker)
            {
                var host = Dns.GetHostName();
                Console.WriteLine($"From environment: IP NULL, running in docker container, defaulting to: {host}");
                remoteOptions.PublicHostName = host.ToHocon();
            }
            else
            {
                Console.WriteLine("From environment: IP NULL, not running in docker container, defaulting to: localhost");
                remoteOptions.PublicHostName = "localhost";
            }

            if (options.Seeds is not null)
            {
                var seeds = string.Join(",", options.Seeds.Select(s => s.ToHocon()));
                clusterOptions.SeedNodes = options.Seeds;
                Console.WriteLine($"From environment: SEEDS: [{seeds}]");
            }
            else
            {
                Console.WriteLine($"From environment: SEEDS: NULL, using seeds: [{string.Join(", ", clusterOptions.SeedNodes ?? new []{ "" })}]");
            }

            var managementPort = 8558;
            if (options.ManagementPort is not null)
            {
                managementPort = options.ManagementPort.Value;
                Console.WriteLine($"From environment: MANAGEMENTPORT: [{managementPort}]");
            }
            else
            {
                Console.WriteLine($"From environment: MANAGEMENTPORT NULL, defaulting to: {managementPort}");
            }

            if (options.ReadinessPort is not null)
            {
                readinessPort = options.ReadinessPort.Value;
                Console.WriteLine($"From environment: READINESSPORT: [{readinessPort}]");
            }
            else
            {
                Console.WriteLine($"From environment: READINESSPORT NULL, defaulting to: {readinessPort}");
            }

            if (options.PbmPort is not null)
            {
                pbmPort = options.PbmPort.Value;
                Console.WriteLine($"From environment: PBMPORT: [{pbmPort}]");
            }
            else
            {
                Console.WriteLine($"From environment: PBMPORT NULL, defaulting to: {pbmPort}");
            }
            
            #endregion

            builder
                .AddHocon(config.GetSection("Akka"), HoconAddMode.Prepend)
                .WithRemoting(remoteOptions)
                .WithClustering(clusterOptions)
                .AddPetabridgeCmd(new PetabridgeCmdOptions
                {
                    Host = "0.0.0.0",
                    Port = pbmPort
                }, pbm =>
                {
                    // enable cluster management commands
                    pbm.RegisterCommandPalette(ClusterCommands.Instance); 
                })
                // Not explicitly setting the liveness provider. The Akka.Remote port
                // is usually an effective-enough tool for this.
                .WithHealthCheck(opt =>
                {
                    opt.Readiness.Transport = HealthCheckTransport.Tcp;
                    opt.Readiness.TcpPort = readinessPort;
                });

            // No need to setup seed based cluster
            if (options.StartupMethod is null or StartupMethod.SeedNodes)
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
                    setup.Http.Port = managementPort;
                    setup.Http.BindHostName = "0.0.0.0";
                    setup.Http.BindPort = managementPort;
                });
                
                // Add Akka.Management.Cluster.Bootstrap support
                builder.WithClusterBootstrap(setup =>
                {
                    setup.ContactPointDiscovery.ServiceName = options.Discovery.ServiceName;
                    setup.ContactPointDiscovery.RequiredContactPointsNr = 3;
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

            #region Azure discovery setup

            if (options.StartupMethod is StartupMethod.AzureDiscovery)
            {
                Console.WriteLine("From environment: Forming cluster using Akka.Discovery.Azure");
                
                builder.WithAkkaManagement(setup =>
                {
                    setup.Http.HostName = options.Ip ?? "localhost";
                    setup.Http.Port = managementPort;
                    setup.Http.BindHostName = "0.0.0.0";
                    setup.Http.BindPort = managementPort;
                });
                
                // Add Akka.Management.Cluster.Bootstrap support
                var connectionString = config.GetConnectionString("azure-tables");
                builder
                    .WithClusterBootstrap(setup =>
                    {
                        setup.ContactPointDiscovery.RequiredContactPointsNr = 3;
                        setup.ContactPointDiscovery.StableMargin = TimeSpan.FromSeconds(5);
                        setup.ContactPointDiscovery.ContactWithAllContactPoints = true;
                        setup.ContactPointDiscovery.ServiceName = options.Discovery.ServiceName;
                        setup.ContactPoint.FilterOnFallbackPort = false;
                    }, autoStart: true)
                    .WithAzureDiscovery(opt =>
                    {
                        opt.ConnectionString = connectionString;
                        opt.ServiceName = options.Discovery.ServiceName;
                        opt.Port = managementPort;
                    })
                    .AddHocon(AzureDiscovery.DefaultConfiguration(), HoconAddMode.Append);
                Console.WriteLine($"From environment: Akka.Discovery.Azure Service Name: {options.Discovery.ServiceName}");
                return builder;
            }
            
            #endregion
            
            #region Kubernetes discovery setup
            if (options.StartupMethod is not StartupMethod.KubernetesDiscovery)
                throw new ConfigurationException($"From environment: Unknown startup method: {options.StartupMethod}");

            Console.WriteLine("From environment: Forming cluster using Akka.Discovery.KubernetesApi");
            
            builder.WithAkkaManagement(setup =>
            {
                setup.Http.HostName = "";
                setup.Http.Port = managementPort;
            });
                
            // Add Akka.Management.Cluster.Bootstrap support
            builder
                .WithClusterBootstrap(setup =>
                {
                    setup.ContactPointDiscovery.PortName = "management";
                    setup.ContactPointDiscovery.RequiredContactPointsNr = 3;
                    setup.ContactPointDiscovery.StableMargin = TimeSpan.FromSeconds(5);
                    setup.ContactPointDiscovery.ContactWithAllContactPoints = true;
                    setup.ContactPointDiscovery.ServiceName = options.Discovery.ServiceName;
                }, autoStart: true)
                .WithKubernetesDiscovery(opt =>
                {
                    opt.PodNamespace = options.Discovery.ServiceName;
                    opt.PodLabelSelector = "cluster={0}";
                })
                .AddHocon(KubernetesDiscovery.DefaultConfiguration(), HoconAddMode.Append);
            
            return builder;
            #endregion
        }

        private static ClusterOptions GetEnvironmentVariables(IConfiguration configuration)
        {
            var section = configuration.GetSection("Cluster");
            if(!section.GetChildren().Any())
            {
                Console.WriteLine("Skipping environment variable bootstrap. No 'CLUSTER' section found");
                return new ClusterOptions();
            }
            
            var options = section.Get<ClusterOptions>();
            if (options is null)
            {
                Console.WriteLine("Skipping environment variable bootstrap. Could not bind IConfiguration to 'DockerOptions'");
                return new ClusterOptions();
            }

            return options;
        }
    }
}