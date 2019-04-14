// -----------------------------------------------------------------------
// <copyright file="HttpClientFactory.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.Net.Http;

namespace WebCrawler.Shared.IO
{
    /// <summary>
    ///     Factory class for creating <see cref="System.Net.Http.HttpClient" /> instances
    /// </summary>
    public static class HttpClientFactory
    {
        public static HttpClient GetClient()
        {
            return new HttpClient();
        }
    }
}