using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using TheBestLogger;
using TheBestLogger.Examples;
using Debug = UnityEngine.Debug;

public class GameLoggerSample : MonoBehaviour
{
    private void Start()
    {
        //RunTest();
    }

    private async void RunTest()
    {
        print(Application.persistentDataPath);
        GameLogger.GameLoading.LogInfo("StartTest", new LogAttributes(LogImportance.Critical));

        await Task.Delay(100);
        GameLogger.GameLoading.LogDebug("debugtest1");
        await Task.Delay(100);
        GameLogger.GameLoading.LogDebug("debugtest2");
        await Task.Delay(100);

        var dict = LogManager.GetCurrentLogTargetConfigurations();
        dict[nameof(UnityEditorConsoleLogTargetConfiguration)].MinLogLevel = LogLevel.Warning;
        LogManager.UpdateLogTargetsConfigurations(dict);

        GameLogger.GameLoading.LogDebug("debugtest3");
        GameLogger.GameLoading.LogWarning("warningtest3");
        await Task.Delay(100);
        GameLogger.GameLoading.LogDebug("debugtest4");
        GameLogger.GameLoading.LogWarning("warningtest4");

        Debug.LogError("Some unity debuglogerror2");
        await Task.Delay(1000);
       
        var dict2 = LogManager.GetCurrentLogTargetConfigurations();
        dict2[nameof(UnityEditorConsoleLogTargetConfiguration)].MinLogLevel = LogLevel.Debug;
        LogManager.UpdateLogTargetsConfigurations(dict);
        
        try
        {
            throw new ArgumentException("handled exception on main thread3");
        }
        catch (Exception ex)
        {
            GameLogger.GameLoading.LogException(ex);
        }

        GameLogger.GameLoading.LogDebug("some debug log4");
        await Task.Delay(20);

        GameLogger.GameLoading.LogWarning(
            "some warning5", new LogAttributes("testkey2", 2)
                             .Add("booleankey", false)
                             .Add("stringkey", "the best string ever"));

        Debug.LogWarning("Some unity debuglogwarning6");

        Start2("backthread exception7");

        throw new ArgumentException("Unhandled exception on main thread8");
    }

    private void OnApplicationQuit()
    {
        LogManager.Dispose();
    }

    private void Start2(string message)
    {
        //ThreadPool.QueueUserWorkItem(o => Throw("from thread pool"));
        //new Thread(() => ThrowAsync("from thread")).Start();
        Task.Run(() => ThrowAsync(message)); //.Wait();
    }

    private void ThrowAsync(string message)
    {
        try
        {
            Debug.Log($"Log about next ThrowAsync() throwing exception log: {message}");
            throw new Exception($"ThrowAsync() Exception: {message}");
        }
        catch (Exception ex)
        {
            GameLogger.Main.LogException(ex);
        }
    }

    /*private async void ThrowAsync(string message)
    {
        await Task.Delay(1).ConfigureAwait(true); // Result is the same with or without this Delay
       
        Debug.Log($"ThrowAsync() throwing exception {message}");
        throw new Exception($"ThrowAsync() Exception {message}");
    }*/

    private void Throw(string message)
    {
        Debug.Log($"Throw() throwing exception {message}");
        throw new Exception($"Throw() Exception {message}");
    }

    private static void CreateFaultedTask()
    {
        // Create a task that will throw an exception
        Task.Run(() => { throw new InvalidOperationException("test UnobservedTaskException Task failure!"); });

        // The exception from the above task is never observed (not awaited or caught)
    }
}
