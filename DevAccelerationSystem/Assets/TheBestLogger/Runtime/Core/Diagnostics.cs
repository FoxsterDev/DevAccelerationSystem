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
    internal class Diagnostics
    {
        private readonly LogLevel _minLogLevel;
        private string _logsDirectory;
        private int _mainThreadId;
        private static readonly Lazy<Diagnostics> _instance = new Lazy<Diagnostics>(() => new Diagnostics());
        private static Diagnostics Instance => _instance.Value;

        private ConcurrentDictionary<string, FileBackgroundAsyncWriter> _writers = new ConcurrentDictionary<string, FileBackgroundAsyncWriter>(2, 4);
        private FileBackgroundAsyncWriter GetWriter(string writerId)
        {
            if (!_writers.TryGetValue(writerId, out var writer))
            {
                writer = new FileBackgroundAsyncWriter(_logsDirectory, writerId+".txt");
                _writers.TryAdd(writerId, writer);
                return writer;
            }

            return writer;
        }

      /// <summary>
      /// Should be called from main thread
      /// </summary>
      /// <param name="minLogLevel"></param>
        private Diagnostics(LogLevel minLogLevel = LogLevel.Debug)
        {
#if LOGGER_DIAGNOSTICS_ENABLED
            _minLogLevel = minLogLevel;
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            //not thread safe
            var rootDirectory = Application.persistentDataPath;
            _logsDirectory = Path.Combine(rootDirectory, "DiagnosticLoggers", $"logs_{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
#endif
        }

        [Conditional(LoggerScriptingDefineSymbols.LOGGER_DIAGNOSTICS_ENABLED)]
        public static void Cancel()
        {
            Instance.Dispose();
        }

        private void Dispose()
        {
            Write("begin");
            if (_writers != null)
            {
                foreach (var wr in _writers)
                {
                    wr.Value?.Dispose();
                }

                //_writers = null;
            }
        }

        [Conditional(LoggerScriptingDefineSymbols.LOGGER_DIAGNOSTICS_ENABLED)]
        public static void Write(string message,
                                 LogLevel level = LogLevel.Debug,
                                 Exception ex = null,
                                 string writerId = null,
                                 [CallerMemberName] string memberName = "",
                                 [CallerFilePath] string filePath = "",
                                 [CallerLineNumber] int lineNumber = 0)
        {
            if (level < Instance._minLogLevel)
            {
                return;
            }

            var exString = string.Empty;
            if (ex != null)
            {
                exString = $"\n\n-->Exception message: {ex?.Message}\nStacktrace:{ex?.StackTrace}";
            }

            var mainThread = Thread.CurrentThread.ManagedThreadId == Instance._mainThreadId
                                 ? ""
                                 : "BackThread";
            var fileName = string.IsNullOrEmpty(filePath)
                               ? "Unknown File"
                               : Path.GetFileName(filePath);
            var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {level}: {fileName}:{lineNumber}-[{memberName}][{mainThread}:{Thread.CurrentThread.ManagedThreadId}]-->{message} {exString}";
            if (string.IsNullOrEmpty(writerId))
            {
                writerId = "Main";
            }

            Instance.GetWriter(writerId)?.Write(logEntry);
        }
    }
}
