using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using TheBestLogger.Core.Utilities;

namespace TheBestLogger
{
    internal class LogTargetDispatchingLogsToMainThreadDecoration : ILogTarget
    {
        private const int MAX_PENDING_DISPATCHES = 1024;
        private const int MAX_DISPATCHES_PER_TICK = 64;
        private const int TARGET_FAILURES_BEFORE_MUTE = 3;
        private const string DROPPED_DISPATCHES_ATTRIBUTE = "DroppedDispatchesCount";

        private readonly ConcurrentQueue<PendingDispatch> _importantDispatches = new();
        private readonly ConcurrentQueue<PendingDispatch> _niceToHaveDispatches = new();
        private readonly ILogTarget _original;
        private readonly UtilitySupplier _utilitySupplier;
        private int _importantDispatchCount;
        private int _niceToHaveDispatchCount;
        private int _droppedDispatchCount;
        private int _drainScheduled;
        private int _disposed;
        private int _targetFailureCount;
        private LogTargetDispatchingLogsToMainThreadConfiguration _config;
        private SynchronizationContext _unityContext;

        public LogTargetDispatchingLogsToMainThreadDecoration(LogTargetDispatchingLogsToMainThreadConfiguration config,
                                                              ILogTarget original,
                                                              SynchronizationContext unityContext,
                                                              UtilitySupplier utilitySupplier)
        {
            _config = config;
            _original = original;
            _unityContext = unityContext;
            _utilitySupplier = utilitySupplier;
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _disposed, 1);
            ClearQueue(_importantDispatches, ref _importantDispatchCount);
            ClearQueue(_niceToHaveDispatches, ref _niceToHaveDispatchCount);
            _original.Dispose();
            _unityContext = null;
        }

        LogTargetConfiguration ILogTarget.Configuration => _original.Configuration;
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
            if (_config.Enabled && _config.SingleLogDispatchEnabled && !_utilitySupplier.IsMainThread)
            {
                Enqueue(PendingDispatch.ForLog(level, category, message, logAttributes, exception),
                        logAttributes.LogImportance == LogImportance.NiceToHave);
                return;
            }

            _original.Log(level, category, message, logAttributes, exception);
        }

        void ILogTarget.LogBatch(IReadOnlyList<LogEntry> logBatch)
        {
            if (_config.Enabled && _config.BatchLogsDispatchEnabled && !_utilitySupplier.IsMainThread)
            {
                if (logBatch == null || logBatch.Count < 1)
                {
                    return;
                }

                EnqueueBatch(logBatch);
                return;
            }

            _original.LogBatch(logBatch);
        }

        void ILogTarget.ApplyConfiguration(LogTargetConfiguration configuration)
        {
            _original.ApplyConfiguration(configuration);
            _config = configuration.DispatchingLogsToMainThread;
        }

        bool ILogTarget.DebugModeEnabled
        {
            get => _original.DebugModeEnabled;
            set => _original.DebugModeEnabled = value;
        }

        private void Enqueue(PendingDispatch pendingDispatch, bool niceToHave)
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                return;
            }

            if (niceToHave)
            {
                _niceToHaveDispatches.Enqueue(pendingDispatch);
                Interlocked.Add(ref _niceToHaveDispatchCount, pendingDispatch.Weight);
            }
            else
            {
                _importantDispatches.Enqueue(pendingDispatch);
                Interlocked.Add(ref _importantDispatchCount, pendingDispatch.Weight);
            }

            TrimOverflow();
            ScheduleDrain();
        }

        private void EnqueueBatch(IReadOnlyList<LogEntry> logBatch)
        {
            for (var offset = 0; offset < logBatch.Count; offset += MAX_DISPATCHES_PER_TICK)
            {
                var count = Math.Min(MAX_DISPATCHES_PER_TICK, logBatch.Count - offset);
                var batchSegment = new LogEntry[count];
                for (var index = 0; index < count; index++)
                {
                    batchSegment[index] = logBatch[offset + index];
                }

                Enqueue(PendingDispatch.ForBatch(batchSegment), false);
            }
        }

        private void ScheduleDrain()
        {
            if (Interlocked.CompareExchange(ref _drainScheduled, 1, 0) != 0)
            {
                return;
            }

            var unityContext = _unityContext;
            if (unityContext == null)
            {
                Interlocked.Exchange(ref _drainScheduled, 0);
                Interlocked.Increment(ref _droppedDispatchCount);
                return;
            }

            try
            {
                unityContext.Post(_ => DrainPendingDispatches(), null);
            }
            catch (Exception exception)
            {
                Interlocked.Exchange(ref _drainScheduled, 0);
                Diagnostics.Write("Failed to schedule main-thread log dispatch.", LogLevel.Error, exception);
            }
        }

        private void DrainPendingDispatches()
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                Interlocked.Exchange(ref _drainScheduled, 0);
                return;
            }

            var processedCount = 0;
            var droppedDispatchCount = Interlocked.Exchange(ref _droppedDispatchCount, 0);
            if (droppedDispatchCount > 0)
            {
                var attributes = new LogAttributes(LogImportance.Important)
                    .Add(DROPPED_DISPATCHES_ATTRIBUTE, droppedDispatchCount);
                if (!TryExecute(PendingDispatch.ForLog(LogLevel.Warning,
                                                       "TheBestLogger",
                                                       StringOperations.Concat("Dropped ", droppedDispatchCount,
                                                                               " pending main-thread log dispatches."),
                                                       attributes,
                                                       null)))
                {
                    FinishDrain();
                    return;
                }
            }

            while (processedCount < MAX_DISPATCHES_PER_TICK &&
                   TryTakeNext(MAX_DISPATCHES_PER_TICK - processedCount, out var pendingDispatch))
            {
                if (!TryExecute(pendingDispatch))
                {
                    FinishDrain();
                    return;
                }

                processedCount += pendingDispatch.Weight;
            }

            if (HasPendingDispatches())
            {
                var unityContext = _unityContext;
                if (unityContext != null)
                {
                    try
                    {
                        unityContext.Post(_ => DrainPendingDispatches(), null);
                        return;
                    }
                    catch (Exception exception)
                    {
                        Diagnostics.Write("Failed to continue main-thread log dispatch.", LogLevel.Error, exception);
                    }
                }
            }

            FinishDrain();
        }

        private bool TryExecute(PendingDispatch pendingDispatch)
        {
            try
            {
                if (pendingDispatch.Batch != null)
                {
                    _original.LogBatch(pendingDispatch.Batch);
                }
                else
                {
                    _original.Log(pendingDispatch.Entry.Level,
                                  pendingDispatch.Entry.Category,
                                  pendingDispatch.Entry.Message,
                                  pendingDispatch.Entry.Attributes,
                                  pendingDispatch.Entry.Exception);
                }

                return true;
            }
            catch (Exception exception)
            {
                Diagnostics.Write("Main-thread log dispatch target failed: " + _original.GetType().Name,
                                  LogLevel.Error,
                                  exception);
                var failureCount = Interlocked.Increment(ref _targetFailureCount);
                if (failureCount < TARGET_FAILURES_BEFORE_MUTE)
                {
                    return true;
                }

                try
                {
                    _original.Mute(true);
                }
                catch (Exception)
                {
                }

                ClearQueue(_importantDispatches, ref _importantDispatchCount);
                ClearQueue(_niceToHaveDispatches, ref _niceToHaveDispatchCount);
                Interlocked.Exchange(ref _droppedDispatchCount, 0);
                return false;
            }
        }

        private void FinishDrain()
        {
            Interlocked.Exchange(ref _drainScheduled, 0);
            if (HasPendingDispatches())
            {
                ScheduleDrain();
            }
        }

        private void TrimOverflow()
        {
            while (Volatile.Read(ref _importantDispatchCount) + Volatile.Read(ref _niceToHaveDispatchCount) >
                   MAX_PENDING_DISPATCHES)
            {
                if (!TryDropOne(_niceToHaveDispatches, ref _niceToHaveDispatchCount, out var droppedWeight) &&
                    !TryDropOne(_importantDispatches, ref _importantDispatchCount, out droppedWeight))
                {
                    return;
                }

                Interlocked.Add(ref _droppedDispatchCount, droppedWeight);
            }
        }

        private bool TryTakeNext(int maxWeight, out PendingDispatch pendingDispatch)
        {
            if (_importantDispatches.TryPeek(out pendingDispatch))
            {
                if (pendingDispatch.Weight > maxWeight || !_importantDispatches.TryDequeue(out pendingDispatch))
                {
                    pendingDispatch = default;
                    return false;
                }

                Interlocked.Add(ref _importantDispatchCount, -pendingDispatch.Weight);
                return true;
            }

            if (_niceToHaveDispatches.TryPeek(out pendingDispatch))
            {
                if (pendingDispatch.Weight > maxWeight || !_niceToHaveDispatches.TryDequeue(out pendingDispatch))
                {
                    pendingDispatch = default;
                    return false;
                }

                Interlocked.Add(ref _niceToHaveDispatchCount, -pendingDispatch.Weight);
                return true;
            }

            pendingDispatch = default;
            return false;
        }

        private bool HasPendingDispatches()
        {
            return Volatile.Read(ref _importantDispatchCount) > 0 || Volatile.Read(ref _niceToHaveDispatchCount) > 0;
        }

        private static bool TryDropOne(ConcurrentQueue<PendingDispatch> queue,
                                       ref int count,
                                       out int droppedWeight)
        {
            if (!queue.TryDequeue(out var pendingDispatch))
            {
                droppedWeight = 0;
                return false;
            }

            droppedWeight = pendingDispatch.Weight;
            Interlocked.Add(ref count, -droppedWeight);
            return true;
        }

        private static void ClearQueue(ConcurrentQueue<PendingDispatch> queue, ref int count)
        {
            while (queue.TryDequeue(out _))
            {
            }

            Interlocked.Exchange(ref count, 0);
        }

        private readonly struct PendingDispatch
        {
            private PendingDispatch(LogEntry entry, IReadOnlyList<LogEntry> batch)
            {
                Entry = entry;
                Batch = batch;
            }

            internal LogEntry Entry { get; }
            internal IReadOnlyList<LogEntry> Batch { get; }
            internal int Weight => Batch?.Count ?? 1;

            internal static PendingDispatch ForLog(LogLevel level,
                                                   string category,
                                                   string message,
                                                   LogAttributes logAttributes,
                                                   Exception exception)
            {
                return new PendingDispatch(new LogEntry(level, category, message, logAttributes, exception), null);
            }

            internal static PendingDispatch ForBatch(IReadOnlyList<LogEntry> logBatch)
            {
                return new PendingDispatch(default, logBatch);
            }
        }
    }
}
