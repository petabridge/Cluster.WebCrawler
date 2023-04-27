// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.Threading.Tasks;
using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.Remote.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebCrawler.Shared.DevOps;

namespace WebCrawler.CrawlService
{
    internal class Program
    {
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
                            .AddHocon(hocon: "akka.remote.dot-netty.tcp.maximum-frame-size = 256000b", addMode: HoconAddMode.Prepend)
                            // Add common DevOps settings
                            .WithOps(
                                remoteOptions: new RemoteOptions
                                {
                                    HostName = "0.0.0.0",
                                    Port = 5213
                                },
                                clusterOptions: new ClusterOptions
                                {
                                    SeedNodes = new [] { "akka.tcp://webcrawler@localhost:16666" },
                                    Roles = new [] { "crawler" }
                                }, 
                                config: hostContext.Configuration,
                                readinessPort: 11001,
                                pbmPort: 9110);
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