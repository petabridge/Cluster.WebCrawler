// -----------------------------------------------------------------------
// <copyright file="HoconLoader.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.IO;
using Akka.Configuration;

namespace WebCrawler.Shared.Config
{
    /// <summary>
    ///     Used to load <see cref="WebCrawler.Shared.Config" /> objects from stand-alone HOCON files.
    /// </summary>
    public static class HoconLoader
    {
        public static Akka.Configuration.Config ParseConfig(string hoconPath)
        {
            return ConfigurationFactory.ParseString(File.ReadAllText(hoconPath));
        }
    }
}