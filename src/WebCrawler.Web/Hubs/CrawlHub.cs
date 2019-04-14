// -----------------------------------------------------------------------
// <copyright file="CrawlHub.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;
using Microsoft.AspNetCore.SignalR;
using WebCrawler.Web.Actors;

namespace WebCrawler.Web.Hubs
{
    public class CrawlHub : Hub
    {
        public void StartCrawl(string message)
        {
            SystemActors.SignalRActor.Tell(message, ActorRefs.Nobody);
        }
    }
}