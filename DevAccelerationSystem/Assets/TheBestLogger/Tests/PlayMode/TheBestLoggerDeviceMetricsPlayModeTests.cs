using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    public class TheBestLoggerDeviceMetricsPlayModeTests
    {
        [UnityTest, Performance]
        public IEnumerator Device_MainThreadBurstAcrossFrames_RecordsFrameTimeAndThroughput()
        {
            const int measurementFrames = 90;
            const int logsPerFrame = 64;

            var target = new DeviceMetricsLogTarget();
            var logger = CreateLogger(target);

            using (Measure.Frames().Scope("Device.MainThreadBurst.FrameTime"))
            {
                for (var frameIndex = 0; frameIndex < measurementFrames; frameIndex++)
                {
                    for (var logIndex = 0; logIndex < logsPerFrame; logIndex++)
                    {
                        logger.LogInfo($"device-main-{frameIndex}-{logIndex}");
                    }

                    yield return null;
                }
            }

            Measure.Custom(new SampleGroup("Device.MainThreadBurst.LoggedCount", SampleUnit.Undefined), target.LoggedCount);
            Measure.Custom(new SampleGroup("Device.MainThreadBurst.LogsPerFrame", SampleUnit.Undefined), (double) target.LoggedCount / measurementFrames);

            Assert.That(target.LoggedCount, Is.EqualTo(measurementFrames * logsPerFrame));
        }

        [UnityTest, Performance]
        public IEnumerator Device_WorkerBurstDispatchAcrossFrames_RecordsFrameTimeAndDelivery()
        {
            const int measurementFrames = 60;
            const int workerCount = 4;
            const int logsPerWorkerPerFrame = 24;

            var target = new DeviceMetricsLogTarget(isThreadSafe: false, dispatchToMainThread: true);
            var syncContext = new DeviceMetricsSynchronizationContext();
            var dispatchingTarget = new LogTargetDispatchingLogsToMainThreadDecoration(
                new LogTargetDispatchingLogsToMainThreadConfiguration
                {
                    Enabled = true,
                    SingleLogDispatchEnabled = true,
                    BatchLogsDispatchEnabled = true
                },
                target,
                syncContext,
                CreateUtilitySupplier());
            var logger = CreateLogger(dispatchingTarget);

            using (Measure.Frames().Scope("Device.WorkerDispatch.FrameTime"))
            {
                for (var frameIndex = 0; frameIndex < measurementFrames; frameIndex++)
                {
                    var exceptions = RunConcurrentBurst(workerCount,
                                                        logsPerWorkerPerFrame,
                                                        (workerId, logIndex) => logger.LogInfo($"device-dispatch-{frameIndex}-{workerId}-{logIndex}"));
                    Assert.That(exceptions, Is.Empty);

                    syncContext.FlushPostedCallbacks();
                    Measure.Custom(new SampleGroup("Device.WorkerDispatch.PendingCallbacks", SampleUnit.Undefined), syncContext.PendingCount);
                    yield return null;
                }
            }

            syncContext.FlushPostedCallbacks();
            Measure.Custom(new SampleGroup("Device.WorkerDispatch.LoggedCount", SampleUnit.Undefined), target.LoggedCount);
            Assert.That(target.LoggedCount, Is.EqualTo(measurementFrames * workerCount * logsPerWorkerPerFrame));
        }

        [UnityTest, Performance]
        public IEnumerator Device_BatchDispatchMixedPressureAcrossFrames_RecordsFrameTimeAndFlushMetrics()
        {
            const int measurementFrames = 75;
            const int workerCount = 4;
            const int logsPerWorkerPerFrame = 16;
            const int updatePeriodMs = 25;

            var innerTarget = new DeviceMetricsLogTarget(isThreadSafe: false, dispatchToMainThread: true);
            var syncContext = new DeviceMetricsSynchronizationContext();
            var utilitySupplier = CreateUtilitySupplier();
            var currentTime = DateTime.UtcNow;
            var dispatchingTarget = new LogTargetDispatchingLogsToMainThreadDecoration(
                new LogTargetDispatchingLogsToMainThreadConfiguration
                {
                    Enabled = true,
                    SingleLogDispatchEnabled = true,
                    BatchLogsDispatchEnabled = true
                },
                innerTarget,
                syncContext,
                utilitySupplier);
            var batchingTarget = new LogTargetBatchLogsDecoration(
                new LogTargetBatchLogsConfiguration
                {
                    Enabled = true,
                    UpdatePeriodMs = updatePeriodMs,
                    MaxCountLogs = 24
                },
                dispatchingTarget,
                currentTime);
            var logger = CreateLogger(batchingTarget);

            using (Measure.Frames().Scope("Device.BatchDispatch.FrameTime"))
            {
                for (var frameIndex = 0; frameIndex < measurementFrames; frameIndex++)
                {
                    var exceptions = RunConcurrentBurst(workerCount,
                                                        logsPerWorkerPerFrame,
                                                        (workerId, logIndex) =>
                                                        {
                                                            var importance = logIndex % 8 == 0
                                                                                 ? LogImportance.Critical
                                                                                 : logIndex % 2 == 0
                                                                                     ? LogImportance.Important
                                                                                     : LogImportance.NiceToHave;
                                                            logger.LogInfo($"device-batch-{frameIndex}-{workerId}-{logIndex}",
                                                                           new LogAttributes(importance));
                                                        });

                    Assert.That(exceptions, Is.Empty);

                    currentTime = currentTime.AddMilliseconds(updatePeriodMs + 5);
                    ((IScheduledUpdate) batchingTarget).Update(currentTime, updatePeriodMs + 5);
                    syncContext.FlushPostedCallbacks();
                    yield return null;
                }
            }

            for (var flushIndex = 0; flushIndex < 4; flushIndex++)
            {
                currentTime = currentTime.AddMilliseconds(updatePeriodMs + 5);
                ((IScheduledUpdate) batchingTarget).Update(currentTime, updatePeriodMs + 5);
                syncContext.FlushPostedCallbacks();
                yield return null;
            }

            Measure.Custom(new SampleGroup("Device.BatchDispatch.LoggedCount", SampleUnit.Undefined), innerTarget.LoggedCount);
            Assert.That(innerTarget.LoggedCount, Is.EqualTo(measurementFrames * workerCount * logsPerWorkerPerFrame));
        }

        private static CoreLogger CreateLogger(params ILogTarget[] targets)
        {
            return new CoreLogger("DeviceMetrics",
                                  string.Empty,
                                  targets,
                                  CreateUtilitySupplier(),
                                  512);
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

        private sealed class DeviceMetricsSynchronizationContext : SynchronizationContext
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

        private sealed class DeviceMetricsLogTarget : LogTarget
        {
            private int _loggedCount;

            public int LoggedCount => _loggedCount;

            public DeviceMetricsLogTarget(bool isThreadSafe = true, bool dispatchToMainThread = false)
            {
                ApplyConfiguration(new DeviceMetricsLogTargetConfiguration
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

            public override string LogTargetConfigurationName => nameof(DeviceMetricsLogTargetConfiguration);

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

        private sealed class DeviceMetricsLogTargetConfiguration : LogTargetConfiguration
        {
        }
    }
}
