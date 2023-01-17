// -----------------------------------------------------------------------
//  <copyright file="ConfigDiscoveryOptions.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2023 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;

namespace WebCrawler.Shared.DevOps.Config;

public sealed class DiscoveryOptions
{
    public string? ServiceName { get; set; }
    public List<string>? ConfigEndpoints { get; set; }
}