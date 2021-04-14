// -----------------------------------------------------------------------
// <copyright file="CommandProcessor.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using Akka.Actor;
using Akka.Routing;
using WebCrawler.Shared.Commands.V1;
using WebCrawler.Shared.State;

namespace WebCrawler.Web.Actors
{
    /// <summary>
    ///     Actor responsible for processing commands
    /// </summary>
    public class CommandProcessor : ReceiveActor
    {
        protected readonly IActorRef CommandRouter;

        public CommandProcessor(IActorRef commandRouter)
        {
            CommandRouter = commandRouter;
            Receives();
        }

        private void Receives()
        {
            Receive<AttemptCrawl>(attempt =>
            {
                if (Uri.IsWellFormedUriString(attempt.RawStr, UriKind.Absolute))
                {
                    var startJob = new StartJob(new CrawlJob(new Uri(attempt.RawStr, UriKind.Absolute), true), Sender);
                    CommandRouter.Tell(startJob);
                    var sender = Sender; // need to close over `Sender` when using `PipeTo`
                    CommandRouter.Ask<Routees>(new GetRoutees()).ContinueWith(tr =>
                    {
                        var grrr =
                            new SignalRActor.DebugCluster(
                                $"{CommandRouter} has {tr.Result.Members.Count()} routees: {string.Join(",", tr.Result.Members.Select(y => y.ToString()))}");

                        return grrr;
                    }).PipeTo(sender);
                    Sender.Tell(startJob);
                }
                else
                {
                    Sender.Tell(new BadCrawlAttempt(attempt.RawStr, "Not an absolute URI"));
                }
            });
        }

        #region Messages

        public class AttemptCrawl
        {
            public AttemptCrawl(string rawStr)
            {
                RawStr = rawStr;
            }

            public string RawStr { get; }
        }

        public class BadCrawlAttempt
        {
            public BadCrawlAttempt(string rawStr, string message)
            {
                Message = message;
                RawStr = rawStr;
            }

            public string RawStr { get; }

            public string Message { get; }
        }

        #endregion
    }
}