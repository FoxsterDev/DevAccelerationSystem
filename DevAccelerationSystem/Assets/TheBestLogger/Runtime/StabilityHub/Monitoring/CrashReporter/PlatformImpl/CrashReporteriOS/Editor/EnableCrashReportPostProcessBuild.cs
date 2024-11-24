using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/*public class PrebuildCallback : IPreprocessBuildWithReport
{
    public int callbackOrder => 1000;
    public void OnPreprocessBuild(BuildReport report)
    {
        
    }
}*/
public class EnableCrashReportPostProcessBuild : IPreprocessBuildWithReport
{
    public int callbackOrder => 1000;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform == BuildTarget.iOS)
        {
            var crashReporterModuleConfiguration = StabilityHub.StabilityHubService.MonitoringConfig.CrashReporterModule;
            if (crashReporterModuleConfiguration.Enabled && crashReporterModuleConfiguration.IOS.Enabled)
            {
                Debug.Log("EnableCrashReportPostProcessBuildfor target " + report.summary.platform + " at path " + report.summary.outputPath);
                EnableCrashReporting();
            }
        }
    }

    private static void EnableCrashReporting()
    {
        PlayerSettings.enableCrashReportAPI = true;

        Debug.Log("Crash Reporting API has been enabled in Player Settings.");
    }
}
