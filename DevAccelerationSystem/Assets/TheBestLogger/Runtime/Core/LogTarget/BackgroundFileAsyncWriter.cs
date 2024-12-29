using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TheBestLogger
{
    public class FileBackgroundAsyncWriter
    {
        private readonly ConcurrentQueue<string> _logQueue;
        private readonly AutoResetEvent _logEvent;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly Task _logTask;
        private volatile bool _disposed;

        public FileBackgroundAsyncWriter(string rootDirectory,
                                         string fileName)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            _logQueue = new ConcurrentQueue<string>();
            _logEvent = new AutoResetEvent(false);

            _logTask = Task.Run(() => ProcessLogQueueAsync(rootDirectory, fileName, _cancellationTokenSource.Token));
        }

        private async Task ProcessLogQueueAsync(string logsDirectory,
                                                string fileName,
                                                CancellationToken cancellationToken)
        {
            if (!Directory.Exists(logsDirectory))
            {
                Directory.CreateDirectory(logsDirectory);
            }

            var logFilePath = Path.Combine(logsDirectory, fileName);

            using (var fileStream =
                   new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read, 4096, true))
            using (var streamWriter = new StreamWriter(fileStream, Encoding.UTF8) { AutoFlush = true })
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    _logEvent.WaitOne(100);

                    while (_logQueue.TryDequeue(out var logEntry))
                    {
                        await streamWriter.WriteLineAsync(logEntry);
                    }
                }

                // Ensure all remaining log entries are flushed when stopping
                while (_logQueue.TryDequeue(out var remainingEntry))
                {
                    await streamWriter.WriteLineAsync(remainingEntry);
                }
            }
        }

        public void Write(string message)
        {
            if (_disposed)
            {
               return;
            }

            _logQueue.Enqueue(message);
            _logEvent.Set(); // Notify the background task that a new log is available
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _ = DisposeAsync().ContinueWith(
                task =>
                {
                    if (task.Exception != null)
                    {
                        UnityEngine.Debug.LogException(task.Exception);
                    }
                }, TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>
        /// not fully thread safe but it is okay for debugging needs
        /// </summary>
        public virtual async Task DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }

            try
            {
                await _logTask;
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
                _logEvent.Dispose();
            }

            GC.SuppressFinalize(this);
        }
    }
}
