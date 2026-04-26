using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using TheBestLogger.Core.Utilities;
using UnityEngine;

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
}
