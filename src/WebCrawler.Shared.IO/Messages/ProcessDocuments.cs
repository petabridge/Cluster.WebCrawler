// -----------------------------------------------------------------------
// <copyright file="ProcessDocuments.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using WebCrawler.Shared.State;

namespace WebCrawler.Shared.IO.Messages
{
    /// <summary>
    ///     Message class used to confirm which documents are available for processing.
    /// </summary>
    public class ProcessDocuments
    {
        public ProcessDocuments(IList<CrawlDocument> documents, IActorRef assigned)
        {
            Assigned = assigned;
            Documents = documents;
        }

        public IList<CrawlDocument> Documents { get; }

        public int HtmlDocs
        {
            get { return Documents.Count(x => !x.IsImage); }
        }

        public int Images
        {
            get { return Documents.Count(x => x.IsImage); }
        }

        /// <summary>
        ///     Reference to the actor who should take on the cleared documents
        /// </summary>
        public IActorRef Assigned { get; }
    }
}