// -----------------------------------------------------------------------
// <copyright file="SignalRActor.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;
using WebCrawler.Shared.Commands.V1;
using WebCrawler.Web.Hubs;

namespace WebCrawler.Web.Actors
{
    /// <summary>
    ///     Actor used to wrap a signalr hub
    /// </summary>
    public class SignalRActor : ReceiveActor, IWithUnboundedStash
    {
        private readonly IActorRef _commandProcessor;

        private readonly CrawlHubHelper _hub;

        public SignalRActor(IActorRef commandProcessor, CrawlHubHelper hub)
        {
            _commandProcessor = commandProcessor;
            _hub = hub;

            HubAvailable();
        }


        public IStash Stash { get; set; }

        private void HubAvailable()
        {
            Receive<string>(str => { _commandProcessor.Tell(new CommandProcessor.AttemptCrawl(str)); });

            Receive<CommandProcessor.BadCrawlAttempt>(bad =>
            {
                _hub.CrawlFailed($"COULD NOT CRAWL {bad.RawStr}: {bad.Message}");
            });

            Receive<IStatusUpdateV1>(status => { _hub.PushStatus(status); });

            Receive<IStartJobV1>(start =>
            {
                _hub.WriteRawMessage($"Starting crawl of {start.Job.Root}");
            });

            Receive<DebugCluster>(debug => { _hub.WriteRawMessage($"DEBUG: {debug.Message}"); });
        }

        public class DebugCluster
        {
            public DebugCluster(string message)
            {
                Message = message;
            }

            public string Message { get; }
        }
    }
}