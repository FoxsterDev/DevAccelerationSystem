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

        [Tooltip("This is period of creating new string formatting timestamp to reduce allocation")]
        public uint MinTimestampPeriodMs = 60;

        [Tooltip("This is used for periodical runupdates for decorations")]
        public uint MinUpdatesPeriodMs = 1000;

        [Tooltip("This is used in NOT Unity editor runtime")]
        public UnityLogTypeStackTraceConfiguration ApplicationLogTypesStackTrace = new();

        // Specifies whether log sources should be enabled or disabled for different scenarios in the Unity Editor or a non-Unity environment.

        /// <summary>
        /// Enables logging for Unity's Debug Log source in the Unity Editor.
        /// Set to true to capture standard Unity debug logs.
        /// </summary>
        [Header("LOG SOURCES FOR UNITY EDITOR RUNTIME")]
        public bool DebugLogSourceUnityEditor = true;

        /// <summary>
        /// Enables logging for application-level logs in the Unity Editor.
        /// Set to true to capture application-specific logs in the Unity Editor.
        /// </summary>
        public bool ApplicationLogSourceUnityEditor = true;

        /// <summary>
        /// Enables logging for threaded application logs in the Unity Editor.
        /// Set to true to capture logs from multi-threaded operations in the Unity Editor.
        /// </summary>
        public bool ApplicationLogSourceThreadedUnityEditor = true;

        /// <summary>
        /// Enables logging for unhandled exceptions in the current application domain in the Unity Editor.
        /// Set to true to capture any unhandled exceptions thrown in the Unity Editor.
        /// </summary>
        public bool CurrentDomainUnhandledExceptionLogSourceUnityEditor = true;

        /// <summary>
        /// Enables logging for unobserved task exceptions in the Unity Editor.
        /// Set to true to capture unobserved exceptions that occur during asynchronous operations in the Unity Editor.
        /// </summary>
        public bool UnobservedTaskExceptionLogSourceUnityEditor = true;

        /// <summary>
        /// Enables logging for system diagnostics debug logs in the Unity Editor.
        /// Set to true to capture system-level diagnostics debug information in the Unity Editor.
        /// </summary>
        [HideInInspector]
        public bool SystemDiagnosticsDebugLogSourceUnityEditor = false;

        /// <summary>
        /// Enables logging for system diagnostics console logs in the Unity Editor.
        /// Set to true to capture system-level diagnostics debug information in the Unity Editor.
        /// </summary>
        public bool SystemDiagnosticsConsoleLogSourceUnityEditor = false;


        /// <summary>
        /// Enables logging for Unity app logs outside of the Unity Editor.  Application.logMessageReceived is used.
        /// Set to true to capture application-specific logs in platform builds.
        /// </summary>
        [Header("LOG SOURCES FOR PLATFORM BUILDS RUNTIME (NOT UNITY EDITOR)")]
        public bool UnityDebugLogSourceForBuildRuntime = false;

        [Tooltip("Enables logging for Unity app logs outside of the Unity Editor.  Application.logMessageReceived is used.\nSet to true to capture application-specific logs in platform builds.")]
        public bool UnityApplicationLogMessageReceivedSourceForBuildRuntime = false;

        /// <summary>
        /// Enables logging for Unity app logs outside of the Unity Editor.  Application.logMessageReceivedThreaded is used.
        /// Set to true to capture application-specific logs in platform builds.
        /// </summary>
        [Tooltip("Enables logging for Unity app threaded logs outside of the Unity Editor. It will catch logs from not unity main thread. Application.logMessageReceivedThreaded is used.\nSet to true to capture application-specific logs in platform builds.")]
        public bool UnityApplicationLogMessageReceivedThreadedSourceForBuildRuntime = true;

        /// <summary>
        /// Enables logging for unobserved task exceptions outside of the Unity Editor.
        /// Set to true to capture unobserved task exceptions that occur during asynchronous operations in platform builds.
        /// </summary>
        public bool SystemThreadingTaskUnobservedTaskExceptionLogSourceForBuildRuntime = true;
    }
}
