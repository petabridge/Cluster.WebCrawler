// -----------------------------------------------------------------------
//  <copyright file="DockerBootstrapAdapter.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Akka.Configuration;
using Akka.Hosting;
using Akka.Hosting.Configuration;
using Microsoft.Extensions.Configuration;

namespace WebCrawler.Shared.DevOps;

public static class DockerBootstrapAdapter
{
    private static readonly Regex SeedValidator = new ("^\\[((\"[a-zA-Z0-9._~:\\/?#\\[\\]@!$&'()*+,;-]*\")(,? *)?)+\\]");
    
    public static AkkaConfigurationBuilder BootstrapFromDocker(this AkkaConfigurationBuilder builder)
    {
        if (!builder.Configuration.HasValue)
            return builder;
        
        var config = builder.Configuration.Value;
        var sb = new StringBuilder();
        
        // Read CLUSTER_IP variable
        var ip = config.GetString("cluster-ip")?.Trim();
        if (!string.IsNullOrEmpty(ip))
            sb.AppendLine($"akka.remote.dot-netty.tcp.public-hostname = {ip.ToHocon()}");
        
        // Read CLUSTER_PORT variable
        var port = config.GetInt("cluster-port", -1);
        if (port != -1)
            sb.AppendLine($"akka.remote.dot-netty.tcp.port = {port}");

        // Read CLUSTER_SEED variable
        var seedValueMaybeArray = config.GetValue("cluster-seeds");
        if (seedValueMaybeArray is { })
        {
            IEnumerable<string> seeds;
            if (seedValueMaybeArray.IsArray())
            {
                seeds = seedValueMaybeArray.GetStringList().Select(s => s.ToHocon());
            } 
            else if (seedValueMaybeArray.IsString())
            {
                var value = seedValueMaybeArray.GetString();
                if(!SeedValidator.IsMatch(value))
                    throw new ConfigurationException($"Invalid array formatting in environment variable 'CLUSTER_SEEDS': {value}");
                seeds = value
                    .TrimStart('[').TrimEnd(']').Split(',')
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.ToHocon());
            }
            else
            {
                throw new ConfigurationException($"Invalid array formatting in environment variable 'CLUSTER_SEEDS': {seedValueMaybeArray.GetString()}");
            }
            sb.AppendLine($"akka.cluster.seed-nodes = [{string.Join(",", seeds)}]");
        }

        if (sb.Length > 0)
            builder.AddHocon(sb.ToString(), HoconAddMode.Prepend);

        return builder;
    }
}