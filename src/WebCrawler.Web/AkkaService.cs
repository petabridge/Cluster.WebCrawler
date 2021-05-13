// -----------------------------------------------------------------------
// <copyright file="AkkaStartupTasks.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.DependencyInjection;
using Akka.Routing;
using Microsoft.Extensions.Hosting;
using WebCrawler.Shared.Config;
using WebCrawler.Shared.DevOps;
using WebCrawler.Web.Actors;

namespace WebCrawler.Web
{
    public interface ISignalRProcessor
    {
        void Deliver(string rawMsg);
    }

    /// <summary>
    /// <see cref="IHostedService"/> that runs and manages <see cref="ActorSystem"/> in background of application.
    /// </summary>
    public sealed class AkkaService : IHostedService, ISignalRProcessor
    {
        private ActorSystem _clusterSystem;
        private readonly IServiceProvider _serviceProvider;
        private IActorRef _signalRActor;

        private readonly IHostApplicationLifetime _applicationLifetime;

        public AkkaService(IServiceProvider serviceProvider, IHostApplicationLifetime appLifetime)
        {
            _serviceProvider = serviceProvider;
            _applicationLifetime = appLifetime;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var config = HoconLoader.ParseConfig("web.hocon");
            var bootstrap = BootstrapSetup.Create()
                .WithConfig(config.ApplyOpsConfig()) // load HOCON and apply extension methods to inject environment variables
                .WithActorRefProvider(ProviderSelection.Cluster.Instance); // launch Akka.Cluster

            // N.B. `WithActorRefProvider` isn't actually needed here - the HOCON file already specifies Akka.Cluster

            // enable DI support inside this ActorSystem, if needed
            var diSetup = ServiceProviderSetup.Create(_serviceProvider);

            // merge this setup (and any others) together into ActorSystemSetup
            var actorSystemSetup = bootstrap.And(diSetup);

            // start ActorSystem
            _clusterSystem = ActorSystem.Create("webcrawler", actorSystemSetup);

            _clusterSystem.StartPbm(); // start Petabridge.Cmd (https://cmd.petabridge.com/)

            // instantiate actors
            var router = _clusterSystem.ActorOf(Props.Empty.WithRouter(FromConfig.Instance), "tasker");
            var processor = _clusterSystem.ActorOf(
                Props.Create(() => new CommandProcessor(router)),
                "commands");
            var signalRProps = ServiceProvider.For(_clusterSystem).Props<SignalRActor>(processor);
            _signalRActor = _clusterSystem.ActorOf(signalRProps, "signalr");

            // add a continuation task that will guarantee shutdown of application if ActorSystem terminates
            _clusterSystem.WhenTerminated.ContinueWith(tr => {
                _applicationLifetime.StopApplication();
            });

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // strictly speaking this may not be necessary - terminating the ActorSystem would also work
            // but this call guarantees that the shutdown of the cluster is graceful regardless
            await CoordinatedShutdown.Get(_clusterSystem).Run(CoordinatedShutdown.ClrExitReason.Instance);
        }

        public void Deliver(string rawMsg)
        {
            _signalRActor.Tell(rawMsg);
        }
    }
}