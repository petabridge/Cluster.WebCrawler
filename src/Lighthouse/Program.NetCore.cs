﻿// -----------------------------------------------------------------------
// <copyright file="Program.NetCore.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Lighthouse
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var lighthouseService = new LighthouseService();
            lighthouseService.Start();
            Console.WriteLine("Press Control + C to terminate.");

            AppDomain.CurrentDomain.ProcessExit += async (sender, eventArgs) =>
            {
                await lighthouseService.StopAsync();
            };

            Console.CancelKeyPress += async (sender, eventArgs) => { await lighthouseService.StopAsync(); };
            lighthouseService.TerminationHandle.Wait();
        }
    }
}