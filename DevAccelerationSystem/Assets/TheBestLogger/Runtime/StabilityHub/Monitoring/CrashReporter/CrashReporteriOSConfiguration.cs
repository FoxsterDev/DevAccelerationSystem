using UnityEngine;

namespace StabilityHub.Monitoring
{
    [System.Serializable]
    internal struct CrashReporteriOSConfiguration
    {
        [Tooltip("Here is used Unity crash reporter api")]
        public bool Enabled;
    }
}
