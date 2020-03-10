// -----------------------------------------------------------------------
// <copyright file="OpsConfig.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using Akka.Configuration;

namespace WebCrawler.Shared.DevOps.Config
{
    /// <summary>
    ///     Holder for shared configuration data used by all WebCrawler services
    /// </summary>
    public class OpsConfig
    {
        /// <summary>
        ///     Name of the <see cref="Environment" /> variable used to look for Phobos
        /// </summary>
        public const string PHOBOS_ENABLED = "PHOBOS_ENABLED";

        /// <summary>
        ///     Name of the <see cref="Environment" /> variable used to direct Phobos' StatsD
        ///     output.
        /// </summary>
        public const string STATSD_URL = "STATSD_URL";

        /// <summary>
        ///     Name of the <see cref="Environment" /> variable used to direct Phobos' StatsD
        ///     output.
        /// </summary>
        public const string STATSD_PORT = "STATSD_PORT";

        public static Akka.Configuration.Config GetOpsConfig()
        {
            return ConfigurationFactory.FromResource<OpsConfig>("WebCrawler.Shared.DevOps.Config.crawler.DevOps.conf");
        }

        public static Akka.Configuration.Config GetPhobosConfig()
        {
            var rawPhobosConfig =
                ConfigurationFactory.FromResource<OpsConfig>("WebCrawler.Shared.DevOps.Config.crawler.Phobos.conf");
            var statsdUrl = Environment.GetEnvironmentVariable(STATSD_URL);
            var statsDPort = Environment.GetEnvironmentVariable(STATSD_PORT);
            if (!string.IsNullOrEmpty(statsdUrl) && int.TryParse(statsDPort, out var portNum))
                return ConfigurationFactory.ParseString($"phobos.monitoring.statsd.endpoint=\"{statsdUrl}\"" +
                                                        Environment.NewLine +
                                                        $"phobos.monitoring.statsd.port={portNum}")
                    .WithFallback(rawPhobosConfig);

            return rawPhobosConfig;
        }
    }
}