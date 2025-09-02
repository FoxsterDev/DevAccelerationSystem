using System;
using TheBestLogger;
using UnityEngine;

namespace StabilityHub.Monitoring
{
    [CreateAssetMenu(
        fileName = nameof(StabilityHub) + nameof(MonitoringConfigurationSO),
        menuName = "ScriptableObjects/StabilityHub/" + nameof(StabilityHub) + nameof(MonitoringConfigurationSO), order = 1)]
    internal sealed class MonitoringConfigurationSO : ScriptableObject
    {
        public CrashReporterModuleConfiguration CrashReporterModule;
        public UnityApplicationEventsTrackingModuleConfiguration UnityApplicationEventsTrackingModule;
        public AppPerformanceTrackingModuleConfiguration AppPerformanceTrackingModule;
    }

    [Serializable]
    internal struct UnityApplicationEventsTrackingModuleConfiguration
    {
        [Tooltip("The full disable or enable the  module")]
        public bool Enabled;

        public bool DeepLinkActivated;
        public bool FocusChanges;
        public bool SceneLoaded;
        public bool SceneUnLoaded;
        public bool ActiveSceneChanged;
    }

    [Serializable]
    internal struct AppPerformanceTrackingModuleConfiguration
    {
        [Tooltip("The full disable or enable the  module")]
        public bool Enabled;

        public LogLevel LogLevelToTrack;
        public int MaxLogEventsOfSomeTypeWithinOneSecond;
        public int MaxTotalLogEventsPerMinute;

        public bool LowMemoryCallback;
        public bool ActiveQualityLevelChanged;

        public float MinBatteryLevelInPercent;

        //// CRITICAL - more 30%/per hout
        public float DrainRateInPercent;
        public float DrainRateTrackingIntervalInSeconds;
#if UNITY_2021_2_OR_NEWER
        /*
         * Android supports Medium, High, and Critical.
           Note: When targeting GameActivity, only Critical is supported.
           iOS supports Critical.
           UWP supports Low, Medium, High, and Critical.
         */
        public ApplicationMemoryUsage MemoryUsage;
#endif
    }
}
