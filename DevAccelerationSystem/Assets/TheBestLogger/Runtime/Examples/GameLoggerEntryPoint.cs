#if LOGGER_AUTO_INITIALIZATION

using System.Collections.Generic;
using System.Threading;
using StabilityHub;
using TheBestLogger;
using TheBestLogger.Examples;
using TheBestLogger.Examples.LogTargets;
using UnityEngine;

public class GameLoggerEntryPoint
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    public static void InitializeFirstLogger()
    {
        var logTargets = new List<LogTarget>
        {
#if UNITY_EDITOR
            new UnityEditorConsoleLogTarget(),
#endif

            new AppleSystemLogTarget(Application.identifier, "Unity"),
            new IMGUIRuntimeLogTarget()
        };

        var cancelToken = CancellationToken.None;
#if UNITY_2022_3_OR_NEWER
        cancelToken = Application.exitCancellationToken;
#endif

        LogManager.Initialize(logTargets.AsReadOnly(), "GameLogger/Dev/", cancelToken);
        StabilityHubService.Initialize(GameLogger.Stability);

        StabilityHubService.RetrieveAndLogPreviousSessionIssues();
    }
}


#endif