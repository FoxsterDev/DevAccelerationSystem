using System;
using System.Collections;
using System.Diagnostics;
using System.Threading.Tasks;

namespace TheBestLogger
{
    public static class TaskExtensions
    {
        private static void LogTaskCancellation(ILogger logger)
        {
            if (logger != null)
            {
                try
                {
                    logger.LogTrace("Task was canceled.");
                    return;
                }
                catch (Exception loggerException)
                {
                    UnityEngine.Debug.LogException(loggerException);
                }
            }

            UnityEngine.Debug.Log("Task was canceled.");
        }

        private static void LogTaskException(ILogger logger, string message, Exception exception)
        {
            if (exception == null)
            {
                UnityEngine.Debug.LogError(message);
                return;
            }

            if (logger != null)
            {
                try
                {
                    logger.LogError(message, exception);
                    return;
                }
                catch (Exception loggerException)
                {
                    UnityEngine.Debug.LogException(loggerException);
                }
            }

            UnityEngine.Debug.LogException(exception);
        }

        private static TaskScheduler GetSafeCurrentSynchronizationContextScheduler()
        {
            try
            {
                return TaskScheduler.FromCurrentSynchronizationContext();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
                return TaskScheduler.Current;
            }
        }

        /// <summary>
        /// Fires a task without waiting for it, and logs any exceptions that occur.
        /// </summary>
        /// <param name="task">The task to run.</param>
        /// <param name="logger"></param>
        public static async void FireAndLogWhenExceptions(this Task task, ILogger logger)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                LogTaskCancellation(logger);
            }
            catch (Exception ex)
            {
                LogTaskException(logger, "Unhandled exception in fire-and-forget task.", ex);
            }
        }

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
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
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
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                try
                {
                    onException?.Invoke(ex);
                }
                catch (Exception callbackException)
                {
                    UnityEngine.Debug.LogException(callbackException);
                }

                UnityEngine.Debug.LogException(ex);
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
                var exception = task.Exception?.Flatten();
                if (exception != null)
                {
                    UnityEngine.Debug.LogException(exception);
                }
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
        public static async Task<T> Retry<T>(Func<Task<T>> taskFactory,
                                             int retryCount,
                                             int delayMilliseconds = 0)
        {
            for (int attempt = 0; attempt <= retryCount; attempt++)
            {
                try
                {
                    return await taskFactory();
                }
                catch (Exception ex) when (attempt < retryCount)
                {
                    UnityEngine.Debug.LogWarning($"Attempt {attempt + 1} failed. Retrying... Exception: {ex.Message}");
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
        public static void HandleExceptions(this Task task,
                                            Action<Exception> onException,
                                            Action onCanceled)
        {
#pragma warning disable VSTHRD110
            task.ContinueWith(
                t =>
                {
                    if (t.IsCanceled)
                    {
                        try
                        {
                            onCanceled?.Invoke();
                        }
                        catch (Exception callbackException)
                        {
                            UnityEngine.Debug.LogException(callbackException);
                        }

                        return;
                    }

                    if (t.IsFaulted)
                    {
                        var ex = t.Exception?.Flatten();
                        try
                        {
                            onException?.Invoke(ex);
                        }
                        catch (Exception callbackException)
                        {
                            UnityEngine.Debug.LogException(callbackException);
                        }
                    }
                }, GetSafeCurrentSynchronizationContextScheduler());
#pragma warning restore VSTHRD110
        }

        /// <summary>
        /// Extension method to handle exceptions for tasks without a return result.
        /// </summary>
        /// <param name="task">The task to handle exceptions for.</param>
        public static void HandleExceptions(this Task task)
        {
#pragma warning disable VSTHRD110
            task.ContinueWith(
                t =>
                {
                    if (t.IsCanceled)
                    {
                        UnityEngine.Debug.Log(string.Concat("Task was canceled. Task ID:", t.Id));
                        return;
                    }

                    if (t.IsFaulted)
                    {
                        var ex = t.Exception?.Flatten();
                        UnityEngine.Debug.LogException(ex);
                    }
                }, TaskScheduler.Current);
#pragma warning restore VSTHRD110
        }

    }
}
