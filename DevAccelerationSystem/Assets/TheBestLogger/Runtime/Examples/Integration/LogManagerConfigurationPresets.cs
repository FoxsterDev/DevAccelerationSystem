using System;
using UnityEngine;

namespace TheBestLogger.Examples
{
    public enum LogManagerConfigurationPreset
    {
        Production = 0,
        Qa = 1
    }

    public static class LogManagerConfigurationPresets
    {
        public static LogManagerConfiguration Create(LogManagerConfigurationPreset preset,
                                                     params LogTargetConfigurationSO[] logTargetConfigs)
        {
            var configuration = ScriptableObject.CreateInstance<LogManagerConfiguration>();
            Apply(configuration, preset);
            configuration.SetLogTargetConfigs(logTargetConfigs);
            return configuration;
        }

        public static LogManagerConfiguration CreateProduction(params LogTargetConfigurationSO[] logTargetConfigs)
        {
            return Create(LogManagerConfigurationPreset.Production, logTargetConfigs);
        }

        public static LogManagerConfiguration CreateQa(params LogTargetConfigurationSO[] logTargetConfigs)
        {
            return Create(LogManagerConfigurationPreset.Qa, logTargetConfigs);
        }

        public static void Apply(LogManagerConfiguration configuration, LogManagerConfigurationPreset preset)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            configuration.DefaultUnityLogsCategoryName = "Uncategorized";
            configuration.MessageMaxLength = 256;
            configuration.MinTimestampPeriodMs = 60;
            configuration.MinUpdatesPeriodMs = 1000;
            configuration.ApplicationLogTypesStackTrace ??= new UnityLogTypeStackTraceConfiguration();
            configuration.StackTraceFormatterConfiguration ??= new Core.Utilities.StackTraceFormatterConfiguration();
            configuration.UniTaskConfiguration ??= new UniTaskConfiguration();
            configuration.RemoteOverrideStartupCache ??= new LogTargetConfigurationCacheSettings();

            switch (preset)
            {
                case LogManagerConfigurationPreset.Production:
                    configuration.DebugUnityLoggerFilterLogType = LogType.Warning;
                    configuration.SetUnityEditorLogSources(
                        unityDebug: true,
                        applicationLogMessageReceived: false,
                        applicationLogMessageReceivedThreaded: true,
                        unobservedSystemTaskException: true,
                        unobservedUniTaskException: true,
                        systemDiagnosticsDebug: false,
                        systemDiagnosticsConsole: false,
                        currentDomainUnhandledException: true);
                    configuration.SetBuildRuntimeLogSources(
                        unityDebug: true,
                        applicationLogMessageReceived: false,
                        applicationLogMessageReceivedThreaded: true,
                        unobservedSystemTaskException: true,
                        unobservedUniTaskException: true,
                        systemDiagnosticsDebug: false,
                        systemDiagnosticsConsole: false,
                        currentDomainUnhandledException: false);
                    break;

                case LogManagerConfigurationPreset.Qa:
                    configuration.DebugUnityLoggerFilterLogType = LogType.Log;
                    configuration.SetUnityEditorLogSources(
                        unityDebug: true,
                        applicationLogMessageReceived: true,
                        applicationLogMessageReceivedThreaded: true,
                        unobservedSystemTaskException: true,
                        unobservedUniTaskException: true,
                        systemDiagnosticsDebug: false,
                        systemDiagnosticsConsole: false,
                        currentDomainUnhandledException: true);
                    configuration.SetBuildRuntimeLogSources(
                        unityDebug: true,
                        applicationLogMessageReceived: true,
                        applicationLogMessageReceivedThreaded: true,
                        unobservedSystemTaskException: true,
                        unobservedUniTaskException: true,
                        systemDiagnosticsDebug: false,
                        systemDiagnosticsConsole: false,
                        currentDomainUnhandledException: true);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
            }
        }
    }
}
