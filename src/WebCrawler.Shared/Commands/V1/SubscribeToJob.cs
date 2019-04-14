// -----------------------------------------------------------------------
// <copyright file="SubscribeToJob.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;
using WebCrawler.Shared.State;

namespace WebCrawler.Shared.Commands.V1
{
    /// <summary>
    ///     Subscribe an actor to a given <see cref="CrawlJob" />
    /// </summary>
    public class SubscribeToJob : ISubscribeToJobV1
    {
        public SubscribeToJob(CrawlJob job, IActorRef subscriber)
        {
            Subscriber = subscriber;
            Job = job;
        }

        public CrawlJob Job { get; }

        public IActorRef Subscriber { get; }
    }
}