// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.DependencyInjection;
using Akka.Hosting;
using Akka.Remote.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebCrawler.Shared.DevOps;
using WebCrawler.TrackerService.Actors;
using WebCrawler.TrackerService.Actors.Tracking;

namespace WebCrawler.TrackerService
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureHostConfiguration(builder =>
                {
                    builder.AddEnvironmentVariables();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddLogging();
                    services.AddAkka("webcrawler", (builder, provider) =>
                    {
                        builder
                            .AddHoconFile("tracker.hocon", HoconAddMode.Prepend)
                            .AddHocon(hocon: "akka.remote.dot-netty.tcp.maximum-frame-size = 256000b", addMode: HoconAddMode.Prepend)
                            .WithRemoting(hostname: "127.0.0.1", port: 0)
                            // Add common DevOps settings
                            .WithOps(
                                clusterOptions: new ClusterOptions
                                {
                                    SeedNodes = new[] { "akka.tcp://webcrawler@localhost:4053" },
                                    Roles = new[] { "tracker" }
                                }, 
                                config: hostContext.Configuration)
                            // instantiate actors
                            .WithActors((system, registry) =>
                            {
                                var apiMaster = system.ActorOf(Props.Create(() => new ApiMaster()), "api");
                                registry.Register<ApiMaster>(apiMaster);
                                
                                var downloadMaster = system.ActorOf(Props.Create(() => new DownloadsMaster()), "downloads");
                                registry.Register<DownloadsMaster>(downloadMaster);
                            });
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