using System;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace TheBestLogger
{
    public abstract class LogTarget : ILogTarget
    {
        private LogLevel _minLogLevel;
        private bool _muted;
        private LogTargetCategory[] _overrideCategories = new LogTargetCategory[0];
        private int _overrideCategoriesCount;
        private string _id;
        private readonly int _hash;
        private bool _debugModeEnabled;
        public abstract string LogTargetConfigurationName { get; }
        public LogTargetConfiguration Configuration { get; private set; }

        public virtual void Mute(bool mute)
        {
            _muted = mute;
        }

        public virtual bool IsStackTraceEnabled(LogLevel level, string category)
        {
            // Check if the LogLevelStackTrace list is not null and that the index is within bounds
            if (Configuration?.StackTraces != null && level >= 0 && (int) level < Configuration.StackTraces.Length)
            {
                return Configuration.StackTraces[(int) level].Enabled;
            }
            else
            {
                return false; // Default to false if there's no valid configuration
            }
        }

        public virtual bool IsLogLevelAllowed(LogLevel logLevel, string category)
        {
            if (_muted) return false;

            if (_debugModeEnabled)
            {
                var debugMode = Configuration.DebugMode;

                var overrideCategoriesCount = debugMode.OverrideCategories != null
                                                  ? debugMode.OverrideCategories.Length
                                                  : 0;
                for (var index = 0; index < overrideCategoriesCount; index++)
                {
                    var logLevelOverride = debugMode.OverrideCategories[index];
                    var areEqual = string.Equals(logLevelOverride.Category, category, StringComparison.Ordinal);
                    if (areEqual)
                    {
                        return (logLevel >= logLevelOverride.MinLevel);
                    }
                }

                return logLevel >= debugMode.MinLogLevel;
            }

            if (_overrideCategoriesCount > 0)
            {
                for (var index = 0; index < _overrideCategoriesCount; index++)
                {
                    var logLevelOverride = _overrideCategories[index];
                    var areEqual = string.Equals(logLevelOverride.Category, category, StringComparison.Ordinal);
                    if (areEqual)
                    {
                        return (logLevel >= logLevelOverride.MinLevel);
                    }
                }
            }

            return (logLevel >= _minLogLevel);
        }

        public abstract void Log(LogLevel level,
                                 string category,
                                 string message,
                                 LogAttributes logAttributes,
                                 Exception exception = null);

        public abstract void LogBatch(
            IReadOnlyList<(LogLevel level, string category, string message, LogAttributes logAttributes, Exception exception)> logBatch);

        public virtual void ApplyConfiguration(LogTargetConfiguration configuration)
        {
            Diagnostics.Write(" begin for " + GetType().Name + " before minLogLevel: " + _minLogLevel + " , new minLogLevel: " + configuration.MinLogLevel);
            Configuration = configuration;
            _minLogLevel = configuration.MinLogLevel;
            _muted = configuration.Muted;
            _overrideCategories = configuration.OverrideCategories;
            _overrideCategoriesCount =
                _overrideCategories != null
                    ? _overrideCategories.Length
                    : 0;

            Diagnostics.Write(" finish " + GetType().Name);
        }

        bool ILogTarget.DebugModeEnabled
        {
            get => _debugModeEnabled;
            set => _debugModeEnabled = value;
        }

        [Preserve]
        protected LogTarget()
        {
            /**/
            _id = GetType().Name;
            _hash = _id.GetHashCode();
        }

        public virtual void Dispose()
        {
        }

        public override int GetHashCode()
        {
            return _hash;
        }

        public override string ToString()
        {
            return _id;
        }
    }
}
