using System;

namespace TheBestLogger
{
    internal interface IScheduledUpdate
    {
        uint PeriodMs { get; }
        
        /// <summary>
        /// Called from Unity main thread
        /// </summary>
        /// <param name="timeDeltaMs">how much time passed from last update</param>
        void Update(DateTime currentTimeUtc, uint timeDeltaMs);
    }
}