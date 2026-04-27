using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace TheBestLogger
{
    [System.Serializable]
    public abstract class LogTargetConfiguration : ISerializationCallbackReceiver
    {
        public bool Muted;
        public LogLevel MinLogLevel = LogLevel.Warning;
        public LogTargetCategory[] OverrideCategories;
        public LogTargetBatchLogsConfiguration BatchLogs;
        public DebugModeConfiguration DebugMode;

        /// <summary>
        /// For performance reasons it will accessed with the int of loglevel, [0] for Debug, [1] for Info, [2] for Warning, [3] for Error, [4] for Exception
        /// </summary>
        [Tooltip(" [0] for Debug, [1] for Info, [2] for Warning, [3] for Error, [4] for Exception")]
        public LogLevelStackTraceConfiguration[] StackTraces;

        public bool IsThreadSafe;
        public LogTargetDispatchingLogsToMainThreadConfiguration DispatchingLogsToMainThread;

        public virtual void Merge(LogTargetConfiguration newConfig)
        {
            Diagnostics.Write(" begin for "+GetType().Name);

            if (newConfig == null) return;

            // Remote patch contract:
            // - null reference fields mean "field absent in remote patch", so the current value must be preserved.
            // - empty arrays mean "explicit clear", so they must overwrite the current value with an empty collection.
            Muted = newConfig.Muted;
            MinLogLevel = newConfig.MinLogLevel;
            if (newConfig.OverrideCategories != null)
                OverrideCategories = newConfig.OverrideCategories;
            if (newConfig.BatchLogs != null) BatchLogs = newConfig.BatchLogs;
            if (newConfig.DebugMode != null) DebugMode = newConfig.DebugMode;
            IsThreadSafe = newConfig.IsThreadSafe;
            const int countOfLogLevels = 5;
            if (newConfig.StackTraces != null && newConfig.StackTraces.Length == countOfLogLevels) StackTraces = newConfig.StackTraces;
            if (newConfig.DispatchingLogsToMainThread != null) DispatchingLogsToMainThread = newConfig.DispatchingLogsToMainThread;
            Diagnostics.Write(" end for "+GetType().Name);
        }

        public void ApplyRuntimeDefaults()
        {
            OverrideCategories ??= Array.Empty<LogTargetCategory>();
            DebugMode ??= new DebugModeConfiguration();
            DebugMode.ApplyRuntimeDefaults();
            BatchLogs ??= new LogTargetBatchLogsConfiguration();
            DispatchingLogsToMainThread ??= new LogTargetDispatchingLogsToMainThreadConfiguration();
            StackTraces ??= CreateDefaultStackTraceConfiguration();
        }

        private static LogLevelStackTraceConfiguration[] CreateDefaultStackTraceConfiguration()
        {
            return new[]
            {
                new LogLevelStackTraceConfiguration { Level = LogLevel.Debug, Enabled = false },
                new LogLevelStackTraceConfiguration { Level = LogLevel.Info, Enabled = false },
                new LogLevelStackTraceConfiguration { Level = LogLevel.Warning, Enabled = false },
                new LogLevelStackTraceConfiguration { Level = LogLevel.Error, Enabled = true },
                new LogLevelStackTraceConfiguration { Level = LogLevel.Exception, Enabled = true }
            };
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            ApplyRuntimeDefaults();
        }
    }
}
