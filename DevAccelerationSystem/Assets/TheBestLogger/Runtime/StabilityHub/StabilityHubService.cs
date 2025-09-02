using System;
using System.Threading;
using StabilityHub.Monitoring;
using UnityEngine;

namespace StabilityHub
{
    public class StabilityHubService : IDisposable
    {
        private static TheBestLogger.ILogger _logger;
        private static string _assetPath;

        private static CrashReporterModule _crashReporterModule;
        private static AppPerformanceTrackingModule _appPerformanceTrackingModule;
        private static CancellationTokenSource _internalCancellationSource;
        private static bool? _isInitialized = null;
        internal static MonitoringConfigurationSO MonitoringConfig => Resources.Load<MonitoringConfigurationSO>(_assetPath);

        public void Dispose()
        {
            _internalCancellationSource?.Dispose();
            _internalCancellationSource = null;
            _crashReporterModule?.Dispose();
            _crashReporterModule = null;
            _appPerformanceTrackingModule?.Dispose();
            _appPerformanceTrackingModule = null;
            _isInitialized = null;
        }

        /// <summary>
        /// Calling from main thread
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="monitoringConfigAssetPath"></param>
        public static void Initialize(TheBestLogger.ILogger logger, string monitoringConfigAssetPath = "StabilityHubMonitoringConfiguration")
        {
            if (_isInitialized.HasValue)
            {
                logger.LogTrace("Already initialized");
                return;
            }

            _logger = logger;
            _assetPath = monitoringConfigAssetPath;
            _internalCancellationSource?.Dispose();
            _internalCancellationSource = new CancellationTokenSource();
            var configSo = MonitoringConfig;

            if (configSo == null)
            {
                logger.LogError($"StabilityHubMonitoringConfiguration is null from path {_assetPath} but it was called Initialize");
                _isInitialized = false;
                return;
            }

            if (configSo.CrashReporterModule.Enabled)
            {
                _logger.LogDebug("CrashReporterModule is enabled");
                _crashReporterModule = new CrashReporterModule(configSo.CrashReporterModule.IOS.Enabled);
            }
            else
            {
                _logger.LogDebug("CrashReporterModule is disabled");
            }

            if (configSo.AppPerformanceTrackingModule.Enabled)
            {
                _logger.LogTrace("AppPerformanceTrackingModule is enabled");
                _appPerformanceTrackingModule = new AppPerformanceTrackingModule(configSo.AppPerformanceTrackingModule, _logger);
                _ = _appPerformanceTrackingModule.Initialize(_internalCancellationSource.Token);
            }else
            {
                _logger.LogTrace("AppPerformanceTrackingModule is disabled");
            }

            _isInitialized = true;
        }

        public static void RetrieveAndLogPreviousSessionIssues()
        {
            _crashReporterModule?.RetrieveAndLogPreviousSessionIssues(_logger);
        }
    }
}
