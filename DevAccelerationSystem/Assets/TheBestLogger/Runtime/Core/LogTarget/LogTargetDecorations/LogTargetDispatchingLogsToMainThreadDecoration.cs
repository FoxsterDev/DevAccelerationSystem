using System;
using System.Collections.Generic;
using System.Threading;
using TheBestLogger.Core.Utilities;

namespace TheBestLogger
{
    internal class LogTargetDispatchingLogsToMainThreadDecoration : ILogTarget
    {
        private LogTargetDispatchingLogsToMainThreadConfiguration _config;
        private readonly ILogTarget _original;
        private SynchronizationContext _unityContext;
        private readonly UtilitySupplier _utilitySupplier;

        public LogTargetDispatchingLogsToMainThreadDecoration(LogTargetDispatchingLogsToMainThreadConfiguration config,
                                                              ILogTarget original,
                                                              SynchronizationContext unityContext,
                                                              UtilitySupplier utilitySupplier)
        {
            _config = config;
            _original = original;
            _unityContext = unityContext;
            _utilitySupplier = utilitySupplier;
        }

        public void Dispose()
        {
            _original.Dispose();
            _unityContext = null;
        }

        LogTargetConfiguration ILogTarget.Configuration => _original.Configuration;

        string ILogTarget.LogTargetConfigurationName => _original.LogTargetConfigurationName;

        void ILogTarget.Mute(bool mute)
        {
            _original.Mute(mute);
        }

        bool ILogTarget.IsLogLevelAllowed(LogLevel logLevel, string category)
        {
            return _original.IsLogLevelAllowed(logLevel, category);
        }

        bool ILogTarget.IsStackTraceEnabled(LogLevel logLevel, string category)
        {
            return _original.IsStackTraceEnabled(logLevel, category);
        }

        void ILogTarget.Log(LogLevel level,
                            string category,
                            string message,
                            LogAttributes logAttributes,
                            Exception exception)
        {
            if (_config.Enabled && _config.SingleLogDispatchEnabled)
            {
                Diagnostics.Write("_config.Enabled && _config.SingleLogDispatchEnabled");
                if (_unityContext != null && !_utilitySupplier.IsMainThread)
                {
                    Diagnostics.Write("_unityContext.Post: " + message);
                    _unityContext.Post(
                        _ => { _original.Log(level, category, message, logAttributes, exception); }, null);
                    return;
                }
            }

            //Diagnostics.Write("Skipped single log dispatching");
            _original.Log(level, category, message, logAttributes, exception);
        }

        void ILogTarget.LogBatch(IReadOnlyList<(LogLevel level, string category, string message, LogAttributes logAttributes, Exception exception)> logBatch)
        {
            if (_config.Enabled && _config.BatchLogsDispatchEnabled)
            {
                Diagnostics.Write("_config.Enabled && _config.BatchLogsDispatchEnabled");
                if (_unityContext != null && !_utilitySupplier.IsMainThread)
                {
                    if (logBatch == null || logBatch.Count < 1)
                    {
                        Diagnostics.Write("_unityContext.Post: logBatch is null or empty");
                        return;
                    }

                    Diagnostics.Write("_unityContext.Post: logBatch.Count" + logBatch.Count);
                    _unityContext.Post(
                        stateObj => _original.LogBatch(
                            (IReadOnlyList<(LogLevel level, string category, string message, LogAttributes logAttributes, Exception exception)>) stateObj),
                        logBatch);
                    return;
                }
            }

            //Diagnostics.Write("Skipped logbatch dispatching");
            _original.LogBatch(logBatch);
        }

        void ILogTarget.ApplyConfiguration(LogTargetConfiguration configuration)
        {
            _original.ApplyConfiguration(configuration);
            _config = configuration.DispatchingLogsToMainThread;
        }

        bool ILogTarget.DebugModeEnabled
        {
            get => _original.DebugModeEnabled;
            set => _original.DebugModeEnabled = value;
        }
    }
}
