// -----------------------------------------------------------------------
// <copyright file="CrawlHub.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;
using Akka.Hosting;
using Microsoft.AspNetCore.SignalR;
using WebCrawler.Web.Actors;

namespace WebCrawler.Web.Hubs
{
    public class CrawlHub : Hub
    {
        private readonly IActorRef _signalRActor;

        public CrawlHub(IRequiredActor<SignalRActor> actor)
        {
            _signalRActor = actor.ActorRef;
        }

        public void StartCrawl(string message)
        {
            _signalRActor.Tell(message);
        }
    }
}