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
        private readonly ISignalRProcessor _processor;

        public CrawlHub(ISignalRProcessor processor)
        {
            _processor = processor;
        }

        public void StartCrawl(string message)
        {
            _processor.Deliver(message);
        }
    }
}