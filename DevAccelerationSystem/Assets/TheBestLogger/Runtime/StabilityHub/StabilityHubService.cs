using System;
using StabilityHub.Monitoring;
using TheBestLogger;
using UnityEngine;
using UnityEngine.Scripting;

namespace StabilityHub
{
    public class CrashReporteriOS : IDisposable
    {
        [Preserve]
        public CrashReporteriOS()
        {

        }
        public  void RetrieveAndLogPreviousSessionIssues(TheBestLogger.ILogger logger)
        {
            //thread safe
            var reports = CrashReport.reports;
            if (reports.Length > 0)
            {
                logger.LogError("Crash was detected!", new LogAttributes(LogImportance.Critical));
                CrashReport.RemoveAll();
            }
        }

        public void Dispose()
        {

        }
    }

    public class CrashReporterModule : IDisposable
    {
        public  void RetrieveAndLogPreviousSessionIssues(TheBestLogger.ILogger logger)
        {

        }
        private readonly CrashReporteriOS _crashReporteriOS;

        [Preserve]
        public CrashReporterModule(bool iOSModuleEnabled)
        {
            if (iOSModuleEnabled)
            {
                _crashReporteriOS = new CrashReporteriOS();
            }
        }

        public void Dispose()
        {
            _crashReporteriOS?.Dispose();
        }
    }
    public class StabilityHubService : IDisposable
    {
        private static TheBestLogger.ILogger _logger;

        internal static  MonitoringConfiguration MonitoringConfig
        {
            get { return UnityEngine.Resources.Load<MonitoringConfiguration>(nameof(StabilityHub)+nameof(MonitoringConfiguration)); }
        }

        private static CrashReporterModule crashReporterModule;

        public static void RetrieveAndLogPreviousSessionIssues()
        {
            crashReporterModule?.RetrieveAndLogPreviousSessionIssues(_logger);
        }

        /// <summary>
        /// Calling from main thread
        /// </summary>
        /// <param name="logger"></param>
        public static void Initialize(TheBestLogger.ILogger logger)
        {
            _logger = logger;
            var crashReporterModuleEnabled = MonitoringConfig.IsIOSCrashReporterModuleEnabled;

            if (crashReporterModuleEnabled)
            {
                _logger.LogDebug("CrashReporterModule is enabled");
                //threadsafe
                crashReporterModule = new CrashReporterModule(crashReporterModuleEnabled);
            }
            else
            {
                _logger.LogDebug("CrashReporterModule is disabled");
            }
        }

        public void Dispose()
        {
            crashReporterModule?.Dispose();
        }
    }
}
