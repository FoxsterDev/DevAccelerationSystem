using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using TheBestLogger.Core.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TheBestLogger.Tests.Editor
{
    internal sealed class QueuedSynchronizationContext : SynchronizationContext
    {
        private readonly ConcurrentQueue<(SendOrPostCallback callback, object state)> _queue = new();

        public int PendingCount => _queue.Count;

        public override void Post(SendOrPostCallback d, object state)
        {
            _queue.Enqueue((d, state));
        }

        public void FlushPostedCallbacks()
        {
            while (_queue.TryDequeue(out var item))
            {
                item.callback(item.state);
            }
        }
    }

    internal class MakeSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object state)
        {
            d(state);
        }
    }
    internal class MockUtilitySupplier : UtilitySupplier
    {
        public new bool IsMainThread { get; set; }

        public new ITagsRegistry TagsRegistry { get; }

        public MockUtilitySupplier(uint minTimestampPeriodMs, StackTraceFormatter stackTraceFormatter)
            : base(minTimestampPeriodMs, stackTraceFormatter)
        {
        }

        public MockUtilitySupplier()
            : base(10, new StackTraceFormatter(Application.dataPath, new StackTraceFormatterConfiguration()))
        {
            
        }
    }

    internal class MockLogTargetConfiguration : LogTargetConfiguration
    {
    }

    internal class MockLogTarget : LogTarget
    {
        public List<List<LogEntry>> LoggedBatches { get; } = new();
        public int DisposeCallCount { get; private set; }
        public List<LogEntry> LoggedEntries { get; } = new();

        public MockLogTarget()
        {
            ApplyConfiguration(new MockLogTargetConfiguration
            {
                MinLogLevel = LogLevel.Debug,
                IsThreadSafe = true,
                DebugMode = new DebugModeConfiguration(),
                BatchLogs = new LogTargetBatchLogsConfiguration(),
                DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
            });
        }

        public void SetDebugMode(bool mode)
        {
            ((ILogTarget)(this)).DebugModeEnabled = mode;
        }

        public override string LogTargetConfigurationName => "FakeLogTargetConfiguration";

        public override void Log(LogLevel level,
                                 string category,
                                 string message,
                                 LogAttributes logAttributes,
                                 Exception exception)
        {
            LogBatch(
                new List<LogEntry>
                    { new LogEntry(level, category, message, logAttributes, exception) });
        }

        public override void LogBatch(
            IReadOnlyList<LogEntry> logBatch)
        {
            var batchCopy = new List<LogEntry>(logBatch);
            LoggedBatches.Add(batchCopy);
            LoggedEntries.AddRange(batchCopy);
        }

        public override void Dispose()
        {
            DisposeCallCount++;
        }
    }

    internal sealed class CountingLogTarget : MockLogTarget
    {
    }

    internal sealed class CountingOnlyLogTarget : LogTarget
    {
        private int _loggedCount;
        private int _batchCount;

        public int LoggedCount => _loggedCount;
        public int BatchCount => _batchCount;

        public CountingOnlyLogTarget(bool isThreadSafe = true, bool dispatchToMainThread = false)
        {
            ApplyConfiguration(new MockLogTargetConfiguration
            {
                MinLogLevel = LogLevel.Debug,
                IsThreadSafe = isThreadSafe,
                DebugMode = new DebugModeConfiguration(),
                BatchLogs = new LogTargetBatchLogsConfiguration(),
                DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration
                {
                    Enabled = dispatchToMainThread,
                    SingleLogDispatchEnabled = dispatchToMainThread,
                    BatchLogsDispatchEnabled = dispatchToMainThread
                }
            });
        }

        public override string LogTargetConfigurationName => nameof(MockLogTargetConfiguration);

        public override void Log(LogLevel level,
                                 string category,
                                 string message,
                                 LogAttributes logAttributes,
                                 Exception exception)
        {
            Interlocked.Increment(ref _loggedCount);
        }

        public override void LogBatch(IReadOnlyList<LogEntry> logBatch)
        {
            if (logBatch == null)
            {
                return;
            }

            Interlocked.Increment(ref _batchCount);
            Interlocked.Add(ref _loggedCount, logBatch.Count);
        }
    }

    internal sealed class ConcurrentCaptureLogTarget : LogTarget
    {
        private int _disposeCallCount;
        private int _loggedCount;

        public ConcurrentQueue<LogEntry> LoggedEntries { get; } = new();
        public int LoggedCount => _loggedCount;
        public int DisposeCallCount => _disposeCallCount;

        public ConcurrentCaptureLogTarget(bool isThreadSafe = true, bool dispatchToMainThread = false)
        {
            ApplyConfiguration(new MockLogTargetConfiguration
            {
                MinLogLevel = LogLevel.Debug,
                IsThreadSafe = isThreadSafe,
                DebugMode = new DebugModeConfiguration(),
                BatchLogs = new LogTargetBatchLogsConfiguration(),
                DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration
                {
                    Enabled = dispatchToMainThread,
                    SingleLogDispatchEnabled = dispatchToMainThread,
                    BatchLogsDispatchEnabled = dispatchToMainThread
                }
            });
        }

        public override string LogTargetConfigurationName => nameof(MockLogTargetConfiguration);

        public override void Log(LogLevel level,
                                 string category,
                                 string message,
                                 LogAttributes logAttributes,
                                 Exception exception)
        {
            LoggedEntries.Enqueue(new LogEntry(level, category, message, logAttributes, exception));
            Interlocked.Increment(ref _loggedCount);
        }

        public override void LogBatch(IReadOnlyList<LogEntry> logBatch)
        {
            if (logBatch == null)
            {
                return;
            }

            foreach (var entry in logBatch)
            {
                LoggedEntries.Enqueue(entry);
                Interlocked.Increment(ref _loggedCount);
            }
        }

        public override void Dispose()
        {
            Interlocked.Increment(ref _disposeCallCount);
        }
    }

    internal sealed class CapturedLogCall
    {
        public CapturedLogCall(LogLevel logLevel,
                               string logSourceId,
                               string message,
                               Exception exception,
                               string stackTrace,
                               Object context,
                               object[] args)
        {
            LogLevel = logLevel;
            LogSourceId = logSourceId;
            Message = message;
            Exception = exception;
            StackTrace = stackTrace;
            Context = context;
            Args = args ?? Array.Empty<object>();
        }

        public LogLevel LogLevel { get; }
        public string LogSourceId { get; }
        public string Message { get; }
        public Exception Exception { get; }
        public string StackTrace { get; }
        public Object Context { get; }
        public object[] Args { get; }
    }

    internal sealed class RecordingLogConsumer : ILogConsumer
    {
        private readonly List<CapturedLogCall> _calls = new();
        private readonly object _gate = new();

        public IReadOnlyList<CapturedLogCall> Calls
        {
            get
            {
                lock (_gate)
                {
                    return _calls.ToArray();
                }
            }
        }

        public int Count
        {
            get
            {
                lock (_gate)
                {
                    return _calls.Count;
                }
            }
        }

        public CapturedLogCall LastCall
        {
            get
            {
                lock (_gate)
                {
                    return _calls.Count == 0 ? null : _calls[_calls.Count - 1];
                }
            }
        }

        public void LogFormat(LogLevel logLevel,
                              string logSourceId,
                              string message,
                              Exception exception = null,
                              string stackTrace = null,
                              Object context = null,
                              params object[] args)
        {
            lock (_gate)
            {
                _calls.Add(new CapturedLogCall(logLevel,
                                               logSourceId,
                                               message,
                                               exception,
                                               stackTrace,
                                               context,
                                               args));
            }
        }
    }
}
