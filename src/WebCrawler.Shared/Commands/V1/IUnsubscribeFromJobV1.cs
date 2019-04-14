// -----------------------------------------------------------------------
// <copyright file="IUnsubscribeFromJobV1.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;
using WebCrawler.Shared.State;

namespace WebCrawler.Shared.Commands.V1
{
    public interface IUnsubscribeFromJobV1
    {
        CrawlJob Job { get; }
        IActorRef Subscriber { get; }
    }
}