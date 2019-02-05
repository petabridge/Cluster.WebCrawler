﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using Akka.Actor;
using Akka.Bootstrap.Docker;
using Akka.Configuration;
using ConfigurationException = Akka.Configuration.ConfigurationException;

namespace Lighthouse
{
    /// <summary>
    /// Launcher for the Lighthouse <see cref="ActorSystem"/>
    /// </summary>
    public static class LighthouseHostFactory
    {
        public static ActorSystem LaunchLighthouse()
        {
            var systemName = Environment.GetEnvironmentVariable("ACTORSYSTEM")?.Trim();

            var clusterConfig = ConfigurationFactory.ParseString(File.ReadAllText("akka.hocon"))
                .BootstrapFromDocker();

            var lighthouseConfig = clusterConfig.GetConfig("lighthouse");
            if (lighthouseConfig != null && string.IsNullOrEmpty(systemName))
            {
                systemName = lighthouseConfig.GetString("actorsystem", systemName);
            }

            var ipAddress = clusterConfig.GetString("akka.remote.dot-netty.tcp.public-hostname");
            var port = clusterConfig.GetInt("akka.remote.dot-netty.tcp.port");

            var selfAddress = $"akka.tcp://{systemName}@{ipAddress}:{port}";

            /*
             * Sanity check
             */
            Console.WriteLine($"[Lighthouse] ActorSystem: {systemName}; IP: {ipAddress}; PORT: {port}");
            Console.WriteLine("[Lighthouse] Performing pre-boot sanity check. Should be able to parse address [{0}]", selfAddress);
            selfAddress = new Address("akka.tcp", systemName, ipAddress.Trim(), port).ToString();
            Console.WriteLine("[Lighthouse] Parse successful.");


            var seeds = clusterConfig.GetStringList("akka.cluster.seed-nodes").ToList();

            Config injectedClusterConfigString = null;


            if (!seeds.Contains(selfAddress))
            {
                seeds.Add(selfAddress);

                if (seeds.Count > 1)
                {
                    injectedClusterConfigString = seeds.Aggregate("akka.cluster.seed-nodes = [", (current, seed) => current + (@"""" + seed + @""", "));
                    injectedClusterConfigString += "]";
                }
                else
                {
                    injectedClusterConfigString = "akka.cluster.seed-nodes = [\""+ selfAddress +"\"]";
                }
            }

           

            var finalConfig = injectedClusterConfigString != null ? injectedClusterConfigString
                .WithFallback(clusterConfig) : clusterConfig;

            return ActorSystem.Create(systemName, finalConfig);
        }
    }
}
