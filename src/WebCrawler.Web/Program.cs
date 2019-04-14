// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using Akka.Actor;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using WebCrawler.Web.Actors;

namespace WebCrawler.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = BuildWebHost(args);

            Console.CancelKeyPress += async (sender, eventArgs) =>
            {
                var wait = CoordinatedShutdown.Get(SystemActors.ActorSystem)
                    .Run(CoordinatedShutdown.ClrExitReason.Instance);
                await host.StopAsync(TimeSpan.FromSeconds(10));
                await wait;
            };


            host.Run();
            SystemActors.ActorSystem?.WhenTerminated.Wait();
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .UseKestrel()
                .UseStartup<Startup>()
                .Build();
        }
    }
}