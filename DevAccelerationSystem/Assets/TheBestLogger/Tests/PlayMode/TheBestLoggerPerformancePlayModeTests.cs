using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using TheBestLogger.Core.Utilities;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.TestTools;

namespace TheBestLogger.Tests.PlayMode
{
    [TestFixture]
    public class TheBestLoggerPerformancePlayModeTests
    {
        [UnityTest, Performance]
        public IEnumerator CoreLogger_LogInfoAcrossFrames_RecordsFrameTime()
        {
            const int frameCount = 40;

            var target = new PerformancePlayModeLogTarget();
            var logger = new CoreLogger("RuntimeCategory",
                                        string.Empty,
                                        new ILogTarget[] { target },
                                        CreateUtilitySupplier(),
                                        256);

            using (Measure.Frames().Scope("PlayMode.CoreLogger.LogInfo.FrameTime"))
            {
                for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
                {
                    logger.LogInfo($"frame-{frameIndex}");
                    yield return null;
                }
            }

            Assert.That(target.LoggedCount, Is.EqualTo(frameCount));
        }

        [UnityTest, Performance]
        public IEnumerator BatchAndDispatch_MixedPressureAcrossFrames_RecordsFrameTime()
        {
            const int frameCount = 30;
            const int workerCount = 4;
            const int logsPerWorkerPerFrame = 8;
            const int updatePeriodMs = 20;

            var target = new PerformancePlayModeLogTarget(isThreadSafe: false, dispatchToMainThread: true);
            var syncContext = new PerformancePlayModeQueuedSynchronizationContext();
            var utilitySupplier = CreateUtilitySupplier();
            var dispatchingTarget = new LogTargetDispatchingLogsToMainThreadDecoration(
                new LogTargetDispatchingLogsToMainThreadConfiguration
                {
                    Enabled = true,
                    SingleLogDispatchEnabled = true,
                    BatchLogsDispatchEnabled = true
                },
                target,
                syncContext,
                utilitySupplier);
            var currentTimeUtc = DateTime.UtcNow;
            var batchingTarget = new LogTargetBatchLogsDecoration(
                new LogTargetBatchLogsConfiguration
                {
                    Enabled = true,
                    UpdatePeriodMs = updatePeriodMs,
                    MaxCountLogs = 16
                },
                dispatchingTarget,
                currentTimeUtc);
            var logger = new CoreLogger("RuntimeCategory",
                                        string.Empty,
                                        new ILogTarget[] { batchingTarget },
                                        utilitySupplier,
                                        512);

            using (Measure.Frames().Scope("PlayMode.BatchDispatch.MixedPressure.FrameTime"))
            {
                for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
                {
                    var exceptions = RunConcurrentBurst(workerCount,
                                                        logsPerWorkerPerFrame,
                                                        (workerId, logIndex) =>
                                                        {
                                                            var importance = logIndex % 5 == 0
                                                                                 ? LogImportance.Critical
                                                                                 : logIndex % 2 == 0
                                                                                     ? LogImportance.Important
                                                                                     : LogImportance.NiceToHave;
                                                            logger.LogInfo($"perf-{frameIndex}-{workerId}-{logIndex}",
                                                                           new LogAttributes(importance));
                                                        });

                    Assert.That(exceptions, Is.Empty);

                    currentTimeUtc = currentTimeUtc.AddMilliseconds(updatePeriodMs + 5);
                    ((IScheduledUpdate) batchingTarget).Update(currentTimeUtc, updatePeriodMs + 5);
                    syncContext.FlushPostedCallbacks();

                    yield return null;
                }
            }

            for (var flushIndex = 0; flushIndex < 4; flushIndex++)
            {
                currentTimeUtc = currentTimeUtc.AddMilliseconds(updatePeriodMs + 5);
                ((IScheduledUpdate) batchingTarget).Update(currentTimeUtc, updatePeriodMs + 5);
                syncContext.FlushPostedCallbacks();
                yield return null;
            }

            Assert.That(target.LoggedCount, Is.EqualTo(frameCount * workerCount * logsPerWorkerPerFrame));
        }

        private static UtilitySupplier CreateUtilitySupplier()
        {
            return new UtilitySupplier(0,
                                       new StackTraceFormatter(Application.dataPath, new StackTraceFormatterConfiguration()));
        }

        private static ConcurrentQueue<Exception> RunConcurrentBurst(int workerCount,
                                                                     int logsPerWorker,
                                                                     Action<int, int> action)
        {
            var exceptions = new ConcurrentQueue<Exception>();
            using var startSignal = new ManualResetEventSlim(false);

            var tasks = new Task[workerCount];
            for (var workerId = 0; workerId < workerCount; workerId++)
            {
                var capturedWorkerId = workerId;
                tasks[workerId] = Task.Run(() =>
                {
                    startSignal.Wait();
                    for (var logIndex = 0; logIndex < logsPerWorker; logIndex++)
                    {
                        try
                        {
                            action(capturedWorkerId, logIndex);
                        }
                        catch (Exception exception)
                        {
                            exceptions.Enqueue(exception);
                            break;
                        }
                    }
                });
            }

            startSignal.Set();
            Assert.That(Task.WaitAll(tasks, TimeSpan.FromSeconds(10)), Is.True, "Workers did not finish in time.");
            return exceptions;
        }

        private sealed class PerformancePlayModeQueuedSynchronizationContext : SynchronizationContext
        {
            private readonly ConcurrentQueue<(SendOrPostCallback callback, object state)> _queue = new();

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

        private sealed class PerformancePlayModeLogTarget : LogTarget
        {
            private int _loggedCount;

            public int LoggedCount => _loggedCount;

            public PerformancePlayModeLogTarget(bool isThreadSafe = true, bool dispatchToMainThread = false)
            {
                ApplyConfiguration(new PerformancePlayModeLogTargetConfiguration
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

            public override string LogTargetConfigurationName => nameof(PerformancePlayModeLogTargetConfiguration);

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

                Interlocked.Add(ref _loggedCount, logBatch.Count);
            }
        }

        private sealed class PerformancePlayModeLogTargetConfiguration : LogTargetConfiguration
        {
        }
    }
}
