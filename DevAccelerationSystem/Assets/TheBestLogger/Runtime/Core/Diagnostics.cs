using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace TheBestLogger
{
    public class Diagnostics
    {
        private static readonly Lazy<Diagnostics> _instance = new Lazy<Diagnostics>(() => new Diagnostics());
        private static Diagnostics Instance => _instance.Value;

        private readonly IUtilitySupplier _utilitySupplier;

        private readonly ConcurrentQueue<string> _logQueue;
        private readonly AutoResetEvent _logEvent;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly Task _logTask;

        private Diagnostics(LogLevel minLogLevel = LogLevel.Warning)
        {
#if LOGGER_DIAGNOSTICS_ENABLED
            _cancellationTokenSource = new CancellationTokenSource();

            _logQueue = new ConcurrentQueue<string>();
            _logEvent = new AutoResetEvent(false);

            var rootDirectory = Application.persistentDataPath;

            _logTask = Task.Run(() => ProcessLogQueueAsync(rootDirectory, _cancellationTokenSource.Token));
#endif
        }

        /// <summary>
        /// Enqueue a log message for asynchronous writing to the file.
        /// </summary>
        /// <param name="message">The log message to write.</param>
        private void Log(string message)
        {
            _logQueue.Enqueue(message);
            _logEvent.Set(); // Notify the background task that a new log is available
        }

        /// <summary>
        /// Asynchronously processes the log queue and writes to the file.
        /// </summary>
        /// <param name="rootDirectory"></param>
        /// <param name="cancellationToken">Cancellation token to stop the logging task.</param>
        private async Task ProcessLogQueueAsync(string rootDirectory, CancellationToken cancellationToken)
        {
            var logsDirectory = Path.Combine(rootDirectory, "DiagnosticLoggers");
            if (!Directory.Exists(logsDirectory))
            {
                Directory.CreateDirectory(logsDirectory);
            }

            var logFilePath = Path.Combine(logsDirectory, $"logs_{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}.txt");

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

        [Conditional(LoggerScriptingDefineSymbols.LOGGER_DIAGNOSTICS_ENABLED)]
        public static void Cancel()
        {
            Instance.Dispose();
        }

        private void Dispose()
        {
            Write("begin");
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }

            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }

        [Conditional(LoggerScriptingDefineSymbols.LOGGER_DIAGNOSTICS_ENABLED)]
        public static void Write(string message, LogLevel level = LogLevel.Debug, Exception ex = null, [CallerMemberName] string memberName = "", 
            [CallerFilePath] string filePath = "", 
            [CallerLineNumber] int lineNumber = 0)
        {
            var exString = string.Empty;
            if (ex != null)
            {
                exString = $"-->Exception message: {ex?.Message}\nStacktrace:{ex?.StackTrace}";
            }
            
            // Fallback to "Unknown File" if filePath is not available
            var fileName = string.IsNullOrEmpty(filePath) ? "Unknown File" : Path.GetFileName(filePath);
            var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {level}: {fileName}:{lineNumber}-[{memberName}]-->{message} {exString}";
            Instance.Log(logEntry);
        }
    }
}