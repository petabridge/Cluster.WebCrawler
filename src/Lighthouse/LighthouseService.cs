// -----------------------------------------------------------------------
// <copyright file="LighthouseService.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.Threading.Tasks;
using Akka.Actor;

namespace Lighthouse
{
    public class LighthouseService
    {
        private ActorSystem _lighthouseSystem;

        /// <summary>
        ///     Task completes once the Lighthouse <see cref="ActorSystem" /> has terminated.
        /// </summary>
        /// <remarks>
        ///     Doesn't actually invoke termination. Need to call <see cref="StopAsync" /> for that.
        /// </remarks>
        public Task TerminationHandle => _lighthouseSystem.WhenTerminated;

        public void Start()
        {
            _lighthouseSystem = LighthouseHostFactory.LaunchLighthouse();
        }

        public async Task StopAsync()
        {
            await CoordinatedShutdown.Get(_lighthouseSystem).Run(CoordinatedShutdown.UnknownReason.Instance);
        }
    }
}