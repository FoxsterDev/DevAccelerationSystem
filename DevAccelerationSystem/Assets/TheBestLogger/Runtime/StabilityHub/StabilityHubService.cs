using System;
using StabilityHub.Monitoring;

namespace StabilityHub
{
    public class StabilityHubService : IDisposable
    {
        private static readonly Func<MonitoringConfiguration> DefaultMonitoringConfigLoader =
            () => UnityEngine.Resources.Load<MonitoringConfiguration>(nameof(StabilityHub) + nameof(MonitoringConfiguration));

        private static readonly Func<bool, ICrashReporterModule> DefaultCrashReporterModuleFactory =
            iOSModuleEnabled => new CrashReporterModule(iOSModuleEnabled);

        private static TheBestLogger.ILogger _logger;

        internal static Func<MonitoringConfiguration> MonitoringConfigLoader = DefaultMonitoringConfigLoader;
        internal static Func<bool, ICrashReporterModule> CrashReporterModuleFactory = DefaultCrashReporterModuleFactory;

        internal static MonitoringConfiguration MonitoringConfig
        {
            get { return MonitoringConfigLoader?.Invoke(); }
        }

        private static ICrashReporterModule _crashReporterModule;

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
            Initialize(logger, MonitoringConfig);
        }

        public static void Initialize(TheBestLogger.ILogger logger, MonitoringConfiguration configuration)
        {
            _logger = logger;
            var crashReporterModuleEnabled = configuration != null && configuration.IsIOSCrashReporterModuleEnabled;

            if (crashReporterModuleEnabled)
            {
                _logger.LogDebug("CrashReporterModule is enabled");
                _crashReporterModule = CrashReporterModuleFactory?.Invoke(crashReporterModuleEnabled);
            }
            else
            {
                _logger.LogDebug("CrashReporterModule is disabled");
            }
        }

        internal static void ResetTestHooks()
        {
            MonitoringConfigLoader = DefaultMonitoringConfigLoader;
            CrashReporterModuleFactory = DefaultCrashReporterModuleFactory;
            _crashReporterModule = null;
            _logger = null;
        }

        public void Dispose()
        {
            _crashReporterModule?.Dispose();
            _crashReporterModule = null;
        }
    }
}
