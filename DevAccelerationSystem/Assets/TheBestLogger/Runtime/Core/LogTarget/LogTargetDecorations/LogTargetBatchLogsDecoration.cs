using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace TheBestLogger
{
    public class LogTargetBatchLogsDecoration : ILogTarget, IScheduledUpdate
    {
        private LogTargetBatchLogsConfiguration _config;
        private readonly ILogTarget _original;
        private DateTime _currentTimeUtc;
        private ConcurrentBag<(LogLevel level, string category, string message, LogAttributes logAttributes, Exception exception)> _bagNiceToHaveImportance;
        private ConcurrentBag<(LogLevel level, string category, string message, LogAttributes logAttributes, Exception exception)> _bagRegularImportance;

        uint IScheduledUpdate.PeriodMs => _config.UpdatePeriodMs;

        public LogTargetBatchLogsDecoration(LogTargetBatchLogsConfiguration config, ILogTarget original, DateTime currentTimeUtc)
        {
            _config = config;
            _original = original;
            _bagRegularImportance =
                new ConcurrentBag<(LogLevel level, string category, string message, LogAttributes logAttributes,
                    Exception exception)>();
            _bagNiceToHaveImportance =
                new ConcurrentBag<(LogLevel level, string category, string message, LogAttributes logAttributes,
                    Exception exception)>();
            _currentTimeUtc = currentTimeUtc;
            //UnityEngine.Debug.Log(original.GetType() +" was decorated by " + GetType());
        }

        public void Dispose()
        {
            _original.Dispose();
        }

        public LogTargetConfiguration Configuration => _original.Configuration;

        void ILogTarget.Mute(bool mute)
        {
            _original.Mute(mute);
        }

        bool ILogTarget.IsLogLevelAllowed(LogLevel logLevel, string category)
        {
            return _original.IsLogLevelAllowed(logLevel, category);
        }


        private List<(LogLevel level, string category, string message, LogAttributes logAttributes, Exception
            exception)> GetLogsBatch(int batchSize)
        {
            var batch = new List<(LogLevel level, string category, string message, LogAttributes logAttributes, Exception
                exception)>((int)batchSize);
           
            var count1 = _bagRegularImportance.Count;
            if (count1 > 0)
            {
                if (count1 <= batchSize)
                {
                    batchSize -= count1;
                    batch.AddRange(_bagRegularImportance.ToArray());
                    _bagRegularImportance.Clear();
                }
                else
                {
                    while (batchSize-- > 0 && _bagRegularImportance.TryTake(out var item))
                    {
                        batch.Add(item);
                    }
                }
            }

            if (batchSize > 0)
            {
                var count2 = _bagNiceToHaveImportance.Count;
                if (count2 > 0)
                {
                    if (count2 <= batchSize)
                    {
                        batchSize -= count2;
                        batch.AddRange(_bagNiceToHaveImportance.ToArray());
                        _bagNiceToHaveImportance.Clear();
                    }
                    else
                    {
                        while (batchSize-- > 0 && _bagNiceToHaveImportance.TryTake(out var item))
                        {
                            batch.Add(item);
                        }
                    }
                }
            }

            batch.Reverse();
            return batch;
        }
         
        void ILogTarget.Log(LogLevel level, string category, string message, LogAttributes logAttributes, Exception exception)
        {
            if (logAttributes.LogImportance == LogImportance.Critical)
            {
                //send existing bucket immediately or send just logs
                var batch = GetLogsBatch((int)_config.MaxCountLogs - 1);
                batch.Add(new (level, category, message, logAttributes, exception));
                LogBatch(batch.AsReadOnly());
                return;
            }

            if (logAttributes.LogImportance == LogImportance.NiceToHave)
            {
                _bagNiceToHaveImportance.Add(new(level, category, message, logAttributes, exception));
            }
            else
            {
                _bagRegularImportance.Add(new(level, category, message, logAttributes, exception));
            }
           
            ((IScheduledUpdate)this).Update(logAttributes.TimeUtc, (uint)(logAttributes.TimeUtc - _currentTimeUtc).TotalMilliseconds);
        }

        public void LogBatch(IReadOnlyList<(LogLevel level, string category, string message, LogAttributes logAttributes, Exception exception)> logBatch)
        {
            _original.LogBatch(logBatch);
        }

        void ILogTarget.ApplyConfiguration(LogTargetConfiguration configuration)
        {
            _original.ApplyConfiguration(configuration);
            _config = configuration.BatchLogs;
        }

        public void SetDebugMode(bool isDebugModeEnabled)
        {
            _original.SetDebugMode(isDebugModeEnabled);
        }

        void IScheduledUpdate.Update(DateTime currentTimeUtc, uint timeDeltaMs)
        {
            if (timeDeltaMs >= _config.UpdatePeriodMs || (currentTimeUtc - _currentTimeUtc).TotalMilliseconds >= _config.UpdatePeriodMs)
            {
                _currentTimeUtc = currentTimeUtc;
                var batch = GetLogsBatch((int)_config.MaxCountLogs);
                if (batch.Count > 0)
                {
                    LogBatch(batch.AsReadOnly());
                }
            }
        }
    }
}