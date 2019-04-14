// -----------------------------------------------------------------------
// <copyright file="IStatusUpdateV1.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using WebCrawler.Shared.State;

namespace WebCrawler.Shared.Commands.V1
{
    public interface IStatusUpdateV1
    {
        CrawlJob Job { get; }
        CrawlJobStats Stats { get; }
        DateTime StartTime { get; }
        DateTime? EndTime { get; }
        TimeSpan Elapsed { get; }
        JobStatus Status { get; }
    }
}