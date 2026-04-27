#if THEBESTLOGGER_AUTO_INITIALIZATION
using TheBestLogger.Examples;

public class GameLoggerEntryPoint
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    public static void InitializeFirstLogger()
    {
        GameLoggerExampleBootstrap.Initialize(
            GameLoggerExampleBootstrapMode.ResourcesDev,
            useRuntimeConsoleTarget: true,
            retrievePreviousSessionIssuesOnStart: true);
    }
}

#endif
