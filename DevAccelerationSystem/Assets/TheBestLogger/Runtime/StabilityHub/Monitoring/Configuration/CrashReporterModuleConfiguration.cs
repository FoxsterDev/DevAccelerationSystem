using UnityEngine;
using UnityEngine.Serialization;

namespace StabilityHub.Monitoring
{
    [System.Serializable]
    public struct CrashReporterModuleConfiguration
    {
        [Tooltip("The full disable or enable the crashreporter module")]
        public bool Enabled;
        [FormerlySerializedAs("AutoSetup")]
        [Tooltip("Opt in only when Unity Crash Reporting is the sole native crash reporter. Keep disabled when Firebase Crashlytics, Sentry, Backtrace, or another native crash SDK is installed because their crash handlers are mutually exclusive.")]
        public bool AutoProjectSettingsSetup;
        public CrashReporteriOSConfiguration IOS;
    }
}
