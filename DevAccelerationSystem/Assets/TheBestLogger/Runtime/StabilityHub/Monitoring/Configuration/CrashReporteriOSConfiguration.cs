using UnityEngine;

namespace StabilityHub.Monitoring
{
    [System.Serializable]
    public struct CrashReporteriOSConfiguration
    {
        [Tooltip("Here is used Unity crash reporter api")]
        public bool Enabled;
    }
}
