// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Akka.Bootstrap.Docker;
using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.Remote.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Petabridge.Cmd.Cluster;
using Petabridge.Cmd.Host;
using WebCrawler.Shared.Config;
using WebCrawler.Shared.DevOps;

namespace WebCrawler.CrawlService
{
    internal class Program
    {
        /*
             var config = HoconLoader.ParseConfig("crawler.hocon");
             var bootstrap = BootstrapSetup.Create()
                .WithConfig(config.ApplyOpsConfig()) // load HOCON and apply extension methods to inject environment variables
                .WithActorRefProvider(ProviderSelection.Cluster.Instance); // launch Akka.Cluster

            // N.B. `WithActorRefProvider` isn't actually needed here - the HOCON file already specifies Akka.Cluster

            // enable DI support inside this ActorSystem, if needed
            var diSetup = ServiceProviderSetup.Create(_serviceProvider);

            // merge this setup (and any others) together into ActorSystemSetup
            var actorSystemSetup = bootstrap.And(diSetup);

            // start ActorSystem
            ClusterSystem = ActorSystem.Create("webcrawler", actorSystemSetup);

            ClusterSystem.StartPbm(); // start Petabridge.Cmd (https://cmd.petabridge.com/)
 
         */
        private static async Task Main(string[] args)
        {
            
            var host = new HostBuilder()
                .ConfigureHostConfiguration(config =>
                {
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddLogging();
                    services.AddAkka("webcrawler", (builder, provider) =>
                    {
                        builder
                            .AddHocon("akka.remote.dot-netty.tcp.maximum-frame-size = 256000b", HoconAddMode.Prepend)
                            .WithRemoting(hostname: "127.0.0.1", port: 0)
                            .WithClustering(new ClusterOptions
                            {
                                SeedNodes = new [] { "akka.tcp://webcrawler@localhost:4053" },
                                Roles = new [] { "crawler" }
                            }.WithOps())
                            .WithOps(hostContext.Configuration);
                    });
                })
                .ConfigureLogging((hostContext, configLogging) =>
                {
                    configLogging.AddConsole();
                })
                .UseConsoleLifetime()
                .Build();

            await host.RunAsync();
        }
    }
}