using UnityEngine;

namespace StabilityHub.Monitoring
{
    [CreateAssetMenu(
        fileName = nameof(StabilityHub)+nameof(MonitoringConfiguration),
        menuName = "ScriptableObjects/StabilityHub/"+nameof(StabilityHub)+nameof(MonitoringConfiguration), order = 1)]
    internal sealed class MonitoringConfiguration : ScriptableObject
    {
        public CrashReporterModuleConfiguration CrashReporterModule;
        public bool IsIOSCrashReporterModuleEnabled => CrashReporterModule.Enabled && CrashReporterModule.IOS.Enabled;
    }
}
