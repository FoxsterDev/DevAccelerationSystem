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
            if (report.summary.platform == BuildTarget.iOS)
            {
                var config = StabilityHub.StabilityHubService.MonitoringConfig;
                var crashReporterModuleConfiguration = config.CrashReporterModule;
                if (crashReporterModuleConfiguration.AutoProjectSettingsSetup)
                {
                    Debug.Log(
                        "OnPreprocessBuild " + nameof(CrashReportingPreprocessBuildStep) + " " + report.summary.platform + " at path "
                        + report.summary.outputPath);

                    var enabled = crashReporterModuleConfiguration.Enabled && crashReporterModuleConfiguration.IOS.Enabled;
                    EnableCrashReporting(enabled);
                }
            }
        }

        public static void EnableCrashReporting(bool enabled)
        {
            Debug.Log("Set to  PlayerSettings.enableCrashReportAPI = true");
            PlayerSettings.enableCrashReportAPI = enabled;

            Debug.Log("Crash Reporting API has been enabled in Player Settings.");
        }
    }
}
