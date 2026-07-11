using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using TheBestLogger.Core.Utilities;

namespace TheBestLogger
{
    public class LogTargetBatchLogsDecoration : ILogTarget, IScheduledUpdate
    {
        private const int MIN_BUFFERED_LOGS = 256;
        private const int MAX_BUFFERED_LOGS = 4096;
        private const int BUFFER_TO_BATCH_MULTIPLIER = 4;
        private const string DROPPED_LOGS_CATEGORY = "TheBestLogger";
        private const string DROPPED_LOGS_ATTRIBUTE = "DroppedLogsCount";

        private readonly ThreadLocal<List<LogEntry>> _batchCache = new(() => new List<LogEntry>(10));
        private readonly ConcurrentQueue<LogEntry> _niceToHaveLogs = new();
        private readonly ConcurrentQueue<LogEntry> _regularLogs = new();
        private readonly ILogTarget _original;
        private int _niceToHaveCount;
        private int _regularCount;
        private int _droppedLogsCount;
        private LogTargetBatchLogsConfiguration _config;
        private DateTime _currentTimeUtc;

        public LogTargetBatchLogsDecoration(LogTargetBatchLogsConfiguration config,
                                            ILogTarget original,
                                            DateTime currentTimeUtc)
        {
            _config = config;
            _original = original;
            _currentTimeUtc = currentTimeUtc;
        }

        public void Dispose()
        {
            ClearQueue(_regularLogs, ref _regularCount);
            ClearQueue(_niceToHaveLogs, ref _niceToHaveCount);
            _batchCache.Dispose();
            _original.Dispose();
        }

        public LogTargetConfiguration Configuration => _original.Configuration;
        string ILogTarget.LogTargetConfigurationName => _original.LogTargetConfigurationName;

        void ILogTarget.Mute(bool mute)
        {
            _original.Mute(mute);
        }

        bool ILogTarget.IsLogLevelAllowed(LogLevel logLevel, string category)
        {
            return _original.IsLogLevelAllowed(logLevel, category);
        }

        bool ILogTarget.IsStackTraceEnabled(LogLevel logLevel, string category)
        {
            return _original.IsStackTraceEnabled(logLevel, category);
        }

        void ILogTarget.Log(LogLevel level,
                            string category,
                            string message,
                            LogAttributes logAttributes,
                            Exception exception)
        {
            if (!_config.Enabled)
            {
                _original.Log(level, category, message, logAttributes, exception);
                return;
            }

            if (logAttributes.LogImportance == LogImportance.Critical)
            {
                var batch = GetLogsBatch((int) _config.MaxCountLogs - 1, logAttributes.TimeUtc);
                batch.Add(new LogEntry(level, category, message, logAttributes, exception));
                LogBatch(CreateSnapshot(batch));
                return;
            }

            var entry = new LogEntry(level, category, message, logAttributes, exception);
            if (logAttributes.LogImportance == LogImportance.NiceToHave)
            {
                _niceToHaveLogs.Enqueue(entry);
                Interlocked.Increment(ref _niceToHaveCount);
            }
            else
            {
                _regularLogs.Enqueue(entry);
                Interlocked.Increment(ref _regularCount);
            }

            TrimOverflow();
            ((IScheduledUpdate) this).Update(logAttributes.TimeUtc,
                                             (uint) Math.Max(0, (logAttributes.TimeUtc - _currentTimeUtc).TotalMilliseconds));
        }

        public void LogBatch(IReadOnlyList<LogEntry> logBatch)
        {
            _original.LogBatch(logBatch);
        }

        void ILogTarget.ApplyConfiguration(LogTargetConfiguration configuration)
        {
            _original.ApplyConfiguration(configuration);
            _config = configuration.BatchLogs;
            TrimOverflow();
        }

        bool ILogTarget.DebugModeEnabled
        {
            get => _original.DebugModeEnabled;
            set => _original.DebugModeEnabled = value;
        }

        uint IScheduledUpdate.PeriodMs => _config.Enabled ? _config.UpdatePeriodMs : uint.MaxValue;

        void IScheduledUpdate.Update(DateTime currentTimeUtc, uint timeDeltaMs)
        {
            if (timeDeltaMs >= _config.UpdatePeriodMs ||
                (currentTimeUtc - _currentTimeUtc).TotalMilliseconds >= _config.UpdatePeriodMs)
            {
                _currentTimeUtc = currentTimeUtc;
                var batch = GetLogsBatch((int) _config.MaxCountLogs, currentTimeUtc);
                if (batch.Count > 0)
                {
                    LogBatch(CreateSnapshot(batch));
                }
            }
        }

        private List<LogEntry> GetLogsBatch(int batchSize, DateTime currentTimeUtc)
        {
            var batch = _batchCache.Value;
            batch.Clear();
            if (batchSize <= 0)
            {
                return batch;
            }

            if (batch.Capacity < batchSize)
            {
                batch.Capacity = batchSize;
            }

            var droppedLogsCount = Interlocked.Exchange(ref _droppedLogsCount, 0);
            if (droppedLogsCount > 0)
            {
                var attributes = new LogAttributes(LogImportance.Important)
                {
                    TimeUtc = currentTimeUtc
                };
                attributes.Add(DROPPED_LOGS_ATTRIBUTE, droppedLogsCount);
                batch.Add(new LogEntry(LogLevel.Warning,
                                       DROPPED_LOGS_CATEGORY,
                                       StringOperations.Concat("Dropped ", droppedLogsCount,
                                                               " buffered logs because capacity was exceeded."),
                                       attributes,
                                       null));
            }

            // Control telemetry must not consume the payload budget. Otherwise a batch size of one
            // can permanently starve real logs while the producer remains overloaded.
            var maxDestinationCount = batchSize + batch.Count;
            DrainEntries(_regularLogs, ref _regularCount, batch, maxDestinationCount);
            DrainEntries(_niceToHaveLogs, ref _niceToHaveCount, batch, maxDestinationCount);
            return batch;
        }

        private void TrimOverflow()
        {
            var capacity = GetBufferCapacity();
            while (Volatile.Read(ref _regularCount) + Volatile.Read(ref _niceToHaveCount) > capacity)
            {
                if (!TryDropOne(_niceToHaveLogs, ref _niceToHaveCount) &&
                    !TryDropOne(_regularLogs, ref _regularCount))
                {
                    return;
                }

                Interlocked.Increment(ref _droppedLogsCount);
            }
        }

        private int GetBufferCapacity()
        {
            var batchSize = (int) Math.Min(_config.MaxCountLogs, (uint) MAX_BUFFERED_LOGS);
            return Math.Min(MAX_BUFFERED_LOGS,
                            Math.Max(MIN_BUFFERED_LOGS, batchSize * BUFFER_TO_BATCH_MULTIPLIER));
        }

        private static bool TryDropOne(ConcurrentQueue<LogEntry> queue, ref int count)
        {
            if (!queue.TryDequeue(out _))
            {
                return false;
            }

            Interlocked.Decrement(ref count);
            return true;
        }

        private static void DrainEntries(ConcurrentQueue<LogEntry> source,
                                         ref int sourceCount,
                                         List<LogEntry> destination,
                                         int maxCount)
        {
            while (destination.Count < maxCount && source.TryDequeue(out var item))
            {
                Interlocked.Decrement(ref sourceCount);
                destination.Add(item);
            }
        }

        private static IReadOnlyList<LogEntry> CreateSnapshot(List<LogEntry> batch)
        {
            return batch.ToArray();
        }

        private static void ClearQueue(ConcurrentQueue<LogEntry> queue, ref int count)
        {
            while (queue.TryDequeue(out _))
            {
            }

            Interlocked.Exchange(ref count, 0);
        }
    }
}
