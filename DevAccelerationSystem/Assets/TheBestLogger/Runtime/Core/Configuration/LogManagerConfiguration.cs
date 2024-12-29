#pragma warning disable 0414

#if !UNITY_EDITOR || THEBESTLOGGER_PLATFORM_BUILD_SIMULATION
#define LOGGER_NOT_UNITY_EDITOR
#else
#define LOGGER_UNITY_EDITOR
#endif

using TheBestLogger.Core.Utilities;
using UnityEngine;
using UnityEngine.Serialization;

namespace TheBestLogger
{
    
    [CreateAssetMenu(
        fileName = nameof(LogManagerConfiguration),
        menuName = "ScriptableObjects/Logger/LogManagerConfiguration", order = 1)]
    internal sealed class LogManagerConfiguration : ScriptableObject
    {
        [Tooltip("Set it to Debug.unityLogger.filterLogType")]
        public LogType DebugUnityLoggerFilterLogType = LogType.Log;

        public LogTargetConfigurationSO[] LogTargetConfigs = new LogTargetConfigurationSO[0];

        public string DefaultUnityLogsCategoryName = "Uncategorized";

        [Tooltip("This global settings will be applied before sending into eligible logtargets. Pay attention that logtarget might decorate message with additional parameters. ")]
        public uint MessageMaxLength = 256;

        [Tooltip("This is period of creating new string formatting timestamp to reduce allocation")]
        public uint MinTimestampPeriodMs = 60;

        [Tooltip("This is used for periodical runupdates for decorations")]
        public uint MinUpdatesPeriodMs = 1000;

        [Tooltip("This is used in NOT Unity editor runtime")]
        public UnityLogTypeStackTraceConfiguration ApplicationLogTypesStackTrace = new();

        [Tooltip("Configure stack trace formatting")]
        public StackTraceFormatterConfiguration StackTraceFormatterConfiguration;

        [Tooltip("Configure UniTask")]
        public UniTaskConfiguration UniTaskConfiguration;

        // Specifies whether log sources should be enabled or disabled for different scenarios in the Unity Editor or a non-Unity environment.

        /// <summary>
        /// Enables logging for Unity's Debug Log source in the Unity Editor.
        /// Set to true to capture standard Unity debug logs.
        /// </summary>
        [Header("LOG SOURCES FOR UNITY EDITOR RUNTIME")]
        [SerializeField]
        private bool _unityDebugLogSourceForUnityEditor = true;

        /// <summary>
        /// Enables logging for application-level logs in the Unity Editor.
        /// Set to true to capture application-specific logs in the Unity Editor.
        /// </summary>
        [SerializeField]
        private bool _unityApplicationLogMessageReceivedSourceForUnityEditor = false;

        /// <summary>
        /// Enables logging for threaded application logs in the Unity Editor.
        /// Set to true to capture logs from multi-threaded operations in the Unity Editor.
        /// </summary>
        [SerializeField]
        private bool _unityApplicationLogMessageReceivedThreadedSourceForUnityEditor = true;

        /// <summary>
        /// Enables logging for unobserved task exceptions in the Unity Editor.
        /// Set to true to capture unobserved exceptions that occur during asynchronous operations in the Unity Editor.
        /// </summary>
        [SerializeField]
        private bool _unobservedSystemTaskExceptionLogSourceForUnityEditor = true;

        [SerializeField]
        private bool _unobservedUniTaskExceptionLogSourceForUnityEditor = true;

        /// <summary>
        /// Enables logging for system diagnostics debug logs in the Unity Editor.
        /// Set to true to capture system-level diagnostics debug information in the Unity Editor.
        /// </summary>
        [FormerlySerializedAs("_systemDiagnosticsDebugLogSourceUnityEditor")]
        [HideInInspector]
        [SerializeField]
        private bool _systemDiagnosticsDebugLogSourceForUnityEditor = false;

        /// <summary>
        /// Enables logging for system diagnostics console logs in the Unity Editor.
        /// Set to true to capture system-level diagnostics debug information in the Unity Editor.
        /// </summary>
        [FormerlySerializedAs("_systemDiagnosticsConsoleLogSourceUnityEditor")]
        [SerializeField]
        private bool _systemDiagnosticsConsoleLogSourceForUnityEditor = false;

        /// <summary>
        /// Enables logging for unhandled exceptions in the current application domain in the Unity Editor.
        /// Set to true to capture any unhandled exceptions thrown in the Unity Editor.
        /// </summary>
        [SerializeField]
        private bool _currentDomainUnhandledExceptionLogSourceForUnityEditor = true;

        /// <summary>
        /// Enables logging for Unity app logs outside of the Unity Editor.  Application.logMessageReceived is used.
        /// Set to true to capture application-specific logs in platform builds.
        /// </summary>
        [Header("LOG SOURCES FOR PLATFORM BUILDS RUNTIME (NOT UNITY EDITOR)")]
        [SerializeField]
        private bool _unityDebugLogSourceForBuildRuntime = true;

        [Tooltip(
            "Enables logging for Unity app logs outside of the Unity Editor.  Application.logMessageReceived is used.\nSet to true to capture application-specific logs in platform builds.")]
        [SerializeField]
        private bool _unityApplicationLogMessageReceivedSourceForBuildRuntime = false;

        /// <summary>
        /// Enables logging for Unity app logs outside of the Unity Editor.  Application.logMessageReceivedThreaded is used.
        /// Set to true to capture application-specific logs in platform builds.
        /// </summary>
        [Tooltip(
            "Enables logging for Unity app threaded logs outside of the Unity Editor. It will catch logs from not unity main thread. Application.logMessageReceivedThreaded is used.\nSet to true to capture application-specific logs in platform builds.")]
        [SerializeField]
        private bool _unityApplicationLogMessageReceivedThreadedSourceForBuildRuntime = true;

        /// <summary>
        /// Enables logging for unobserved task exceptions outside of the Unity Editor.
        /// Set to true to capture unobserved task exceptions that occur during asynchronous operations in platform builds.
        /// </summary>
        [SerializeField]
        private bool _unobservedSystemTaskExceptionLogSourceForBuildRuntime = true;

        [SerializeField]
        private bool _unobservedUniTaskExceptionLogSourceForBuildRuntime = true;

        [SerializeField]
        [HideInInspector]
        private bool _systemDiagnosticsConsoleLogSourceForBuildRuntime = false;

        [SerializeField]
        [HideInInspector]
        private bool _systemDiagnosticsDebugLogSourceForBuildRuntime = false;

        [SerializeField]
        [HideInInspector]
        private bool _currentDomainUnhandledExceptionLogSourceForBuildRuntime = false;

        public bool UnityDebugLogSourceEnabled
        {
            get
            {
#if LOGGER_UNITY_EDITOR
                return _unityDebugLogSourceForUnityEditor;
#else
                return _unityDebugLogSourceForBuildRuntime;
#endif
            }
        }

        public bool UnityApplicationLogMessageReceivedSourceEnabled
        {
            get
            {
#if LOGGER_UNITY_EDITOR
                return _unityApplicationLogMessageReceivedSourceForUnityEditor;
#else
                return _unityApplicationLogMessageReceivedSourceForBuildRuntime;
#endif
            }
        }

        public bool UnityApplicationLogMessageReceivedThreadedSourceEnabled
        {
            get
            {
#if LOGGER_UNITY_EDITOR
                return _unityApplicationLogMessageReceivedThreadedSourceForUnityEditor;
#else
                return _unityApplicationLogMessageReceivedThreadedSourceForBuildRuntime;
#endif
            }
        }

        public bool CurrentDomainUnhandledExceptionLogSourceEnabled
        {
            get
            {
#if LOGGER_UNITY_EDITOR
                return _currentDomainUnhandledExceptionLogSourceForUnityEditor;
#else
                return _currentDomainUnhandledExceptionLogSourceForBuildRuntime;
#endif
            }
        }

        public bool UnobservedUniTaskExceptionLogSourceEnabled
        {
            get
            {
#if LOGGER_UNITY_EDITOR
                return _unobservedUniTaskExceptionLogSourceForUnityEditor;
#else
                return _unobservedUniTaskExceptionLogSourceForBuildRuntime;
#endif
            }
        }

        public bool UnobservedSystemTaskExceptionLogSourceEnabled
        {
            get
            {
#if LOGGER_UNITY_EDITOR
                return _unobservedSystemTaskExceptionLogSourceForUnityEditor;
#else
                return _unobservedSystemTaskExceptionLogSourceForBuildRuntime;
#endif
            }
        }

        public bool SystemDiagnosticsDebugLogSourceEnabled
        {
            get
            {
#if LOGGER_UNITY_EDITOR
                return _systemDiagnosticsDebugLogSourceForUnityEditor;
#else
                return _systemDiagnosticsDebugLogSourceForBuildRuntime;
#endif
            }
        }

        public bool SystemDiagnosticsConsoleLogSourceEnabled
        {
            get
            {
#if LOGGER_UNITY_EDITOR
                return _systemDiagnosticsConsoleLogSourceForUnityEditor;
#else
                return _systemDiagnosticsConsoleLogSourceForBuildRuntime;
#endif
            }
        }
    }
}
