// -----------------------------------------------------------------------
// <copyright file="CrawlHubHelper.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using WebCrawler.Shared.Commands.V1;
using WebCrawler.Web.Actors;

namespace WebCrawler.Web.Hubs
{
    /// <summary>
    ///     Necessary for getting access to a hub and passing it along to our actors
    /// </summary>
    public class CrawlHubHelper
    {
        private readonly IHubContext<CrawlHub> _hub;

        public CrawlHubHelper(IHubContext<CrawlHub> hub)
        {
            _hub = hub;
        }

        public void PushStatus(IStatusUpdateV1 update)
        {
            WriteMessage(
                $"[{DateTime.UtcNow}]({update.Job}) - {update.Stats} ({update.Status}) [{update.Elapsed} elapsed]");
        }

        public void CrawlFailed(string reason)
        {
            WriteMessage(reason);
        }

        internal void WriteRawMessage(string msg)
        {
            WriteMessage(msg);
        }

        internal void WriteMessage(string message)
        {
            _hub.Clients.All.SendAsync("writeStatus", message);
        }
    }
}