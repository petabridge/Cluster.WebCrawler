// -----------------------------------------------------------------------
// <copyright file="OpsConfigSpec.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using WebCrawler.Shared.DevOps.Config;
using Xunit;

namespace WebCrawler.Shared.DevOps.Tests.Config
{
    public class OpsConfigSpec
    {
        [Fact]
        public void Should_load_default_OpsConfig()
        {
            var config = OpsConfig.GetOpsConfig();
            config.Should().NotBeNull();
        }
    }
}