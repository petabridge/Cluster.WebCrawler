// -----------------------------------------------------------------------
// <copyright file="ActorSystemStartupSpecs.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Akka.Actor;
using Akka.HealthCheck.Cluster;
using Akka.HealthCheck;
using Akka.HealthCheck.Liveness;
using Akka.HealthCheck.Readiness;
using Akka.TestKit.Xunit2;
using Xunit;
using Xunit.Abstractions;

namespace WebCrawler.Shared.DevOps.Tests
{
    public class ActorSystemStartupSpecs : TestKit
    {
        public ActorSystemStartupSpecs(ITestOutputHelper helper)
            : base(Akka.Configuration.Config.Empty.ApplyOpsConfig(), output: helper)
        {
        }

        [Fact(DisplayName = "Instrumented ActorSystem should start HealthChecks automatically")]
        public void ActorSystem_should_start_HealthChecks_automatically()
        {
            /*
             * Without explicitly invoking the AkkaHealthCheck extension, we should see
             * that the actors responsible for publishing that information have already
             * started and begun broadcasting readiness / liveness status in the background
             * since we enable the extension via the `akka.extensions` HOCON
             */

            // Liveness
            AwaitAssert(() =>
            {
                Sys.ActorSelection("/system/healthcheck-live").Tell(GetCurrentLiveness.Instance);
                ExpectMsg<LivenessStatus>(TimeSpan.FromMilliseconds(30));
            }, TimeSpan.FromSeconds(3), TimeSpan.FromMilliseconds(40));

            // Readiness
            AwaitAssert(() =>
            {
                Sys.ActorSelection("/system/healthcheck-readiness").Tell(GetCurrentReadiness.Instance);
                ExpectMsg<ReadinessStatus>(TimeSpan.FromMilliseconds(30));
            }, TimeSpan.FromSeconds(3), TimeSpan.FromMilliseconds(40));

            // should be running with the Akka.Cluster healthcheck probe
            //AkkaHealthCheck.For(Sys).ReadinessProvider.Should().BeOfType<ClusterReadinessProbeProvider>();
        }
    }
}