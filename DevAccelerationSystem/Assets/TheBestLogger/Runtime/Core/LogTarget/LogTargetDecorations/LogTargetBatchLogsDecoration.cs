using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

//using UnityEngine.EventSystems;

namespace TheBestLogger
{
    public class LogTargetBatchLogsDecoration : ILogTarget, IScheduledUpdate
    {
        private readonly ThreadLocal<List<LogEntry>> _batchCache = new(() => new List<LogEntry>(10));

        private readonly ILogTarget _original;
        private readonly ConcurrentBag<LogEntry> _bagNiceToHaveImportance;
        private readonly ConcurrentBag<LogEntry> _bagRegularImportance;
        private LogTargetBatchLogsConfiguration _config;
        private DateTime _currentTimeUtc;

        public LogTargetBatchLogsDecoration(LogTargetBatchLogsConfiguration config,
                                            ILogTarget original,
                                            DateTime currentTimeUtc)
        {
            _config = config;
            _original = original;
            _bagRegularImportance =
                new ConcurrentBag<LogEntry>();
            _bagNiceToHaveImportance =
                new ConcurrentBag<LogEntry>();
            _currentTimeUtc = currentTimeUtc;
        }

        public void Dispose()
        {
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
                //send existing bucket immediately or send just logs
                var batch = GetLogsBatch((int) _config.MaxCountLogs - 1);
                batch.Add(new LogEntry(level, category, message, logAttributes, exception));
                LogBatch(CreateSnapshot(batch));
                return;
            }

            if (logAttributes.LogImportance == LogImportance.NiceToHave)
            {
                _bagNiceToHaveImportance.Add(new LogEntry(level, category, message, logAttributes, exception));
            }
            else
            {
                _bagRegularImportance.Add(new LogEntry(level, category, message, logAttributes, exception));
            }

            ((IScheduledUpdate) this).Update(logAttributes.TimeUtc, (uint) (logAttributes.TimeUtc - _currentTimeUtc).TotalMilliseconds);
        }

        public void LogBatch(IReadOnlyList<LogEntry> logBatch)
        {
            _original.LogBatch(logBatch);
        }

        void ILogTarget.ApplyConfiguration(LogTargetConfiguration configuration)
        {
            _original.ApplyConfiguration(configuration);
            _config = configuration.BatchLogs;
        }

        bool ILogTarget.DebugModeEnabled
        {
            get => _original.DebugModeEnabled;
            set => _original.DebugModeEnabled = value;
        }

        uint IScheduledUpdate.PeriodMs => Configuration.BatchLogs.UpdatePeriodMs;

        void IScheduledUpdate.Update(DateTime currentTimeUtc, uint timeDeltaMs)
        {
            if (timeDeltaMs >= _config.UpdatePeriodMs || (currentTimeUtc - _currentTimeUtc).TotalMilliseconds >= _config.UpdatePeriodMs)
            {
                _currentTimeUtc = currentTimeUtc;
                var batch = GetLogsBatch((int) _config.MaxCountLogs);
                if (batch.Count > 0)
                {
                    LogBatch(CreateSnapshot(batch));
                }
            }
        }

        private List<LogEntry> GetLogsBatch(int batchSize)
        {
            var batch = _batchCache.Value;
            batch.Clear();
            if (batch.Capacity < batchSize)
            {
                batch.Capacity = batchSize;
            }

            DrainBagEntries(_bagRegularImportance, batch, batchSize);

            if (batch.Count < batchSize)
            {
                DrainBagEntries(_bagNiceToHaveImportance, batch, batchSize - batch.Count);
            }

            return batch;
        }

        private static IReadOnlyList<LogEntry> CreateSnapshot(List<LogEntry> batch)
        {
            return batch.ToArray();
        }

        private static void DrainBagEntries(ConcurrentBag<LogEntry> source,
                                            List<LogEntry> destination,
                                            int maxCount)
        {
            if (maxCount <= 0)
            {
                return;
            }

            var entries = new List<LogEntry>(maxCount);
            while (entries.Count < maxCount && source.TryTake(out var item))
            {
                entries.Add(item);
            }

            entries.Reverse();
            destination.AddRange(entries);
        }
    }
}
