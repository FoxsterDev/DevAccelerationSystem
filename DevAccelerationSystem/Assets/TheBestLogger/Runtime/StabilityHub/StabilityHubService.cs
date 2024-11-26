using System;
using StabilityHub.Monitoring;

namespace StabilityHub
{
    public class StabilityHubService : IDisposable
    {
        private static TheBestLogger.ILogger _logger;

        internal static  MonitoringConfiguration MonitoringConfig
        {
            get { return UnityEngine.Resources.Load<MonitoringConfiguration>(nameof(StabilityHub)+nameof(MonitoringConfiguration)); }
        }

        private static CrashReporterModule _crashReporterModule;

        public static void RetrieveAndLogPreviousSessionIssues()
        {
            _crashReporterModule?.RetrieveAndLogPreviousSessionIssues(_logger);
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
                _crashReporterModule = new CrashReporterModule(crashReporterModuleEnabled);
            }
            else
            {
                _logger.LogDebug("CrashReporterModule is disabled");
            }
        }

        public void Dispose()
        {
            _crashReporterModule?.Dispose();
            _crashReporterModule = null;
        }
    }
}
