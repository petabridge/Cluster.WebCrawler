using System;
using System.Collections.Generic;
using System.Text;
using Akka.Configuration;

namespace WebCrawler.Shared.DevOps.Config
{
    /// <summary>
    /// Holder for shared configuration data used by all WebCrawler services
    /// </summary>
    public class OpsConfig
    {
        public static Akka.Configuration.Config GetOpsConfig()
        {
            return ConfigurationFactory.FromResource<OpsConfig>("WebCrawler.Shared.DevOps.Config.crawler.DevOps.conf");
        }
    }
}
