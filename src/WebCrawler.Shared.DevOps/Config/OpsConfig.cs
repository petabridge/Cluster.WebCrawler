// -----------------------------------------------------------------------
// <copyright file="OpsConfig.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Net;
using System.Text;
using Hocon;
using Newtonsoft.Json.Linq;

namespace WebCrawler.Shared.DevOps.Config
{
    /// <summary>
    ///     Holder for shared configuration data used by all WebCrawler services
    /// </summary>
    public static class OpsConfig
    {
        /// <summary>
        ///     Name of the <see cref="Environment" /> variable used to look for Phobos
        /// </summary>
        private const string PHOBOS_ENABLED = "PHOBOS_ENABLED";

        private static bool? _phobosEnabled = null;
        public static bool PhobosEnabled
        {
            get
            {
                if (_phobosEnabled == null)
                {
                    var enabledPhobosStr = Environment.GetEnvironmentVariable(PHOBOS_ENABLED)?.Trim().ToLowerInvariant() ?? "false";
                    bool.TryParse(enabledPhobosStr, out var value);
                    _phobosEnabled = value;
                }
                return _phobosEnabled.Value;
            }
        }

        public static Hocon.Config GetOpsConfig()
        {
            return HoconConfigurationFactory.FromResource<AssemblyMarker>("WebCrawler.Shared.DevOps.Config.crawler.DevOps.conf");
        }

        public static Hocon.Config GetPhobosConfig()
        {
            // Load the Environment .conf file first so that it is populated with the values from the environment variables,
            // then use .WithFallback() to provide sane default values if these variables are not populated.
            return HoconConfigurationFactory.FromResource<AssemblyMarker>("WebCrawler.Shared.DevOps.Config.crawler.Phobos.conf");
        }
    }
}