using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.Scripting;

namespace TheBestLogger
{
    public abstract class LogTarget : ILogTarget
    {
        private struct CategoryRuntimeState
        {
            public CategoryRuntimeState(LogTargetCategory configuration)
                : this(configuration, string.Empty, 0, 0)
            {
            }

            public CategoryRuntimeState(LogTargetCategory configuration,
                                        string sessionKey,
                                        int configurationApplyVersion,
                                        int categoryIndex)
            {
                Category = configuration?.Category;
                MinLevel = configuration != null ? configuration.MinLevel : LogLevel.Warning;
                IsSessionRolloutActive = configuration != null &&
                                         RolloutSampler.IsRolloutActive(configuration.SessionRolloutPercentage);
                IsEnabledForCurrentSession =
                    !IsSessionRolloutActive ||
                    RolloutSampler.ShouldEnable(sessionKey,
                                                configurationApplyVersion,
                                                categoryIndex,
                                                Category,
                                                configuration.SessionRolloutPercentage);
            }

            public string Category { get; }
            public LogLevel MinLevel { get; }
            public bool IsSessionRolloutActive { get; }
            public bool IsEnabledForCurrentSession { get; }
        }

        private LogLevel _minLogLevel;
        private bool _muted;
        private CategoryRuntimeState[] _overrideCategories = Array.Empty<CategoryRuntimeState>();
        private int _overrideCategoriesCount;
        private CategoryRuntimeState[] _debugOverrideCategories = Array.Empty<CategoryRuntimeState>();
        private int _debugOverrideCategoriesCount;
        private static long _nextCategoryRolloutSessionId;
        private string _id;
        private readonly int _hash;
        private readonly string _categoryRolloutSessionKey;
        private int _configurationApplyVersion;
        private bool _debugModeEnabled;
        public abstract string LogTargetConfigurationName { get; }
        public LogTargetConfiguration Configuration { get; private set; }
        protected bool DebugModeEnabled => _debugModeEnabled;

        internal string CategoryRolloutSessionKey => _categoryRolloutSessionKey;
        internal int NextConfigurationApplyVersion => _configurationApplyVersion + 1;

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
                if (TryEvaluateCategoryOverride(_debugOverrideCategories,
                                                _debugOverrideCategoriesCount,
                                                logLevel,
                                                category,
                                                out var isAllowedByDebugOverride))
                {
                    return isAllowedByDebugOverride;
                }

                return logLevel >= debugMode.MinLogLevel;
            }

            if (TryEvaluateCategoryOverride(_overrideCategories,
                                            _overrideCategoriesCount,
                                            logLevel,
                                            category,
                                            out var isAllowedByOverride))
            {
                return isAllowedByOverride;
            }

            return (logLevel >= _minLogLevel);
        }

        public abstract void Log(LogLevel level,
                                 string category,
                                 string message,
                                 LogAttributes logAttributes,
                                 Exception exception = null);

        public abstract void LogBatch(
            IReadOnlyList<LogEntry> logBatch);

        public virtual void ApplyConfiguration(LogTargetConfiguration configuration)
        {
            configuration?.ApplyRuntimeDefaults();
            Diagnostics.Write(" begin for " + GetType().Name + " before minLogLevel: " + _minLogLevel + " , new minLogLevel: " + configuration.MinLogLevel);

            _configurationApplyVersion++;
            Configuration = configuration;
            _minLogLevel = configuration.MinLogLevel;
            _muted = configuration.Muted;
            _overrideCategories = CreateCategoryRuntimeStates(configuration.OverrideCategories,
                                                              _categoryRolloutSessionKey,
                                                              _configurationApplyVersion,
                                                              out _overrideCategoriesCount);
            _debugOverrideCategories = CreateCategoryRuntimeStates(configuration.DebugMode?.OverrideCategories,
                                                                   _categoryRolloutSessionKey,
                                                                   _configurationApplyVersion,
                                                                   out _debugOverrideCategoriesCount);

            Diagnostics.Write(" finish " + GetType().Name);
        }

        private static bool TryEvaluateCategoryOverride(CategoryRuntimeState[] overrideCategories,
                                                        int overrideCategoriesCount,
                                                        LogLevel logLevel,
                                                        string category,
                                                        out bool isAllowed)
        {
            for (var index = 0; index < overrideCategoriesCount; index++)
            {
                var logLevelOverride = overrideCategories[index];
                if (!string.Equals(logLevelOverride.Category, category, StringComparison.Ordinal))
                {
                    continue;
                }

                if (logLevelOverride.IsSessionRolloutActive && !logLevelOverride.IsEnabledForCurrentSession)
                {
                    isAllowed = false;
                    return true;
                }

                isAllowed = logLevel >= logLevelOverride.MinLevel;
                return true;
            }

            isAllowed = false;
            return false;
        }

        private static CategoryRuntimeState[] CreateCategoryRuntimeStates(LogTargetCategory[] overrideCategories,
                                                                          string sessionKey,
                                                                          int configurationApplyVersion,
                                                                          out int count)
        {
            if (overrideCategories == null || overrideCategories.Length < 1)
            {
                count = 0;
                return Array.Empty<CategoryRuntimeState>();
            }

            var states = new CategoryRuntimeState[overrideCategories.Length];
            count = 0;
            for (var index = 0; index < overrideCategories.Length; index++)
            {
                var overrideCategory = overrideCategories[index];
                if (overrideCategory == null)
                {
                    continue;
                }

                states[count] = new CategoryRuntimeState(overrideCategory, sessionKey, configurationApplyVersion, index);
                count++;
            }

            return states;
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
            var sessionId = Interlocked.Increment(ref _nextCategoryRolloutSessionId);
            _categoryRolloutSessionKey = $"{_id}:{sessionId}:{DateTime.UtcNow.Ticks}";
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
