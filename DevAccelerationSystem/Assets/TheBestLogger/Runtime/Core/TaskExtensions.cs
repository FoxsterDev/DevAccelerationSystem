using System;
using System.Collections;
using System.Threading.Tasks;
using TheBestLogger;
using UnityEngine;
using ILogger = TheBestLogger.ILogger;

public static class TaskExtensions
{
    /// <summary>
    /// Fires a task without waiting for it, and logs any exceptions that occur.
    /// </summary>
    /// <param name="task">The task to run.</param>
    public static async void FireAndForget(this Task task)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            //Debug.Log("Task was canceled.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Unhandled exception in fire-and-forget task: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// Runs a task safely, catching and handling any exceptions.
    /// </summary>
    /// <param name="task">The task to run.</param>
    /// <param name="onException">An optional action to handle exceptions.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task RunSafely(this Task task, Action<Exception> onException = null)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            onException?.Invoke(ex);
            Debug.LogError($"Unhandled exception in safe task: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// Converts an async task to a Unity coroutine.
    /// </summary>
    /// <param name="task">The task to convert.</param>
    /// <returns>An IEnumerator for use with Unity coroutines.</returns>
    public static IEnumerator ToCoroutine(this Task task)
    {
        while (!task.IsCompleted)
        {
            yield return null;
        }

        if (task.IsFaulted)
        {
            var innerException = task.Exception?.Flatten().InnerException;
            if (innerException != null)
                Debug.LogError($"Task faulted with exception: {innerException?.Message}");
        }
    }

    /// <summary>
    /// Retries a task a specified number of times with an optional delay between retries.
    /// </summary>
    /// <typeparam name="T">The type of the result produced by the task.</typeparam>
    /// <param name="taskFactory">A function that returns the task to retry.</param>
    /// <param name="retryCount">The number of retry attempts.</param>
    /// <param name="delayMilliseconds">The delay between retries in milliseconds.</param>
    /// <returns>The result of the task if it succeeds.</returns>
    public static async Task<T> Retry<T>(Func<Task<T>> taskFactory, int retryCount, int delayMilliseconds = 0)
    {
        for (int attempt = 0; attempt <= retryCount; attempt++)
        {
            try
            {
                return await taskFactory();
            }
            catch (Exception ex) when (attempt < retryCount)
            {
                Debug.LogWarning($"Attempt {attempt + 1} failed. Retrying... Exception: {ex.Message}");
                if (delayMilliseconds > 0)
                {
                    await Task.Delay(delayMilliseconds);
                }
            }
        }

        throw new Exception("Task failed after all retry attempts.");
    }
    /// <summary>
    /// Adds a timeout to a task.
    /// </summary>
    /// <typeparam name="T">The type of the result produced by the task.</typeparam>
    /// <param name="task">The task to add a timeout to.</param>
    /// <param name="timeoutMilliseconds">The timeout duration in milliseconds.</param>
    /// <returns>The original task if it completes on time, or throws a TimeoutException if it times out.</returns>
    public static async Task<T> WithTimeout<T>(this Task<T> task, int timeoutMilliseconds)
    {
        if (task == await Task.WhenAny(task, Task.Delay(timeoutMilliseconds)))
        {
            return await task; // Task completed within the timeout
        }
        else
        {
            throw new TimeoutException($"Task timed out after {timeoutMilliseconds} milliseconds.");
        }
    }

    /// <summary>
    /// Extension method to handle exceptions for tasks without a return result.
    /// </summary>
    /// <param name="task">The task to handle exceptions for.</param>
    /// <param name="onException">Action to handle exceptions on the current synchronization context.</param>
    public static void HandleExceptions(this Task task, Action<Exception> onException, Action onCanceled)
    {
#pragma warning disable VSTHRD110
        task.ContinueWith(t =>
        {
            if (t.IsCanceled)
            {
                onCanceled?.Invoke();
                return;
            }

            if (t.IsFaulted)
            {
                var ex = t.Exception?.Flatten().InnerException;
                onException?.Invoke(ex);
            }
        }, TaskScheduler.FromCurrentSynchronizationContext());
#pragma warning restore VSTHRD110
    }
    
    /// <summary>
    /// Extension method to handle exceptions for tasks without a return result.
    /// </summary>
    /// <param name="task">The task to handle exceptions for.</param>
    public static void HandleExceptions(this Task task)
    {
#pragma warning disable VSTHRD110
        task.ContinueWith(t =>
        {
            if (t.IsCanceled)
            {
                UnityEngine.Debug.Log(string.Concat("Task was canceled. Task ID:", t.Id));
                return;
            }
            if (t.IsFaulted)
            {
                var ex = t.Exception?.Flatten().InnerException;
                UnityEngine.Debug.LogException(ex);
            }
        }, TaskScheduler.Current);
#pragma warning restore VSTHRD110
    }

}