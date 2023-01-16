// -----------------------------------------------------------------------
//  <copyright file="DockerOptions.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2023 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

namespace WebCrawler.Shared.DevOps.Config;

public class DockerOptions
{
    public string Ip { get; set; }
    public int? Port { get; set; }
    public string[] Seeds { get; set; }
}