using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace StabilityHub.Monitoring.CrashReporting.Editor
{
    public class CrashReportingPreprocessBuildStep : IPreprocessBuildWithReport
    {
        public int callbackOrder => 1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS)
            {
                return;
            }

            var config = StabilityHub.StabilityHubService.MonitoringConfig;
            if (config == null)
            {
                Debug.LogWarning(nameof(CrashReportingPreprocessBuildStep) +
                                 " skipped crash reporter auto setup because no MonitoringConfiguration asset was loaded.");
                return;
            }

            var crashReporterModuleConfiguration = config.CrashReporterModule;
            if (!crashReporterModuleConfiguration.AutoProjectSettingsSetup)
            {
                return;
            }

            Debug.Log(
                "OnPreprocessBuild " + nameof(CrashReportingPreprocessBuildStep) + " " + report.summary.platform + " at path "
                + report.summary.outputPath);

            EnableCrashReporting(config.IsIOSCrashReporterModuleEnabled);
        }

        public static void EnableCrashReporting(bool enabled)
        {
            Debug.Log("Set to  PlayerSettings.enableCrashReportAPI = true");
            PlayerSettings.enableCrashReportAPI = enabled;

            Debug.Log("Crash Reporting API has been enabled in Player Settings.");
        }
    }
}
