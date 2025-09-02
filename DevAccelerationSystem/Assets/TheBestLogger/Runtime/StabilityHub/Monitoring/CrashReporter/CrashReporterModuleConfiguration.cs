using UnityEngine;
using UnityEngine.Serialization;

namespace StabilityHub.Monitoring
{
    
    [System.Serializable]
    internal struct CrashReporterModuleConfiguration
    {
        [Tooltip("The full disable or enable the crashreporter module")]
        public bool Enabled;
        [Tooltip("If AutoSetup =false you have to manually in build sequence to call CrashReportingPreprocessBuildStep.EnableCrashReporting(). In case true the step will be automatically called")]
        public bool AutoProjectSettingsSetup;
        public CrashReporteriOSConfiguration IOS;
    }
}
