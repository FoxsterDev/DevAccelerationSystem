using System;
using System.Diagnostics;
using NUnit.Framework;
using TheBestLogger.Core.Utilities;
using UnityEngine;

namespace TheBestLogger.Tests.Editor
{
    [TestFixture]
    public class CoreLoggerPerformanceSmokeTests
    {
        private const int HotPathIterations = 2000;
        private const int QueuedLogCount = 2000;

        [Test]
        public void LogInfo_HotPath_StaysWithinBoundedAverageAllocation()
        {
            var target = new CountingOnlyLogTarget();
            var logger = CreateLogger(target);

            WarmUp(() => logger.LogInfo("warmup-message"), 128);
            ForceGarbageCollection();

            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();

            for (var iteration = 0; iteration < HotPathIterations; iteration++)
            {
                logger.LogInfo("constant-message");
            }

            var allocatedAfter = GC.GetAllocatedBytesForCurrentThread();
            var allocatedPerCall = (allocatedAfter - allocatedBefore) / (double) HotPathIterations;

            Assert.That(target.LoggedCount, Is.EqualTo(HotPathIterations + 128));
            Assert.That(allocatedPerCall, Is.LessThan(900d), $"Average allocated bytes per LogInfo call was {allocatedPerCall:F2}.");
        }

        [Test]
        public void LogFormat_HotPath_StaysWithinBoundedAverageAllocation()
        {
            var target = new CountingOnlyLogTarget();
            var logger = CreateLogger(target);

            WarmUp(() => logger.LogFormat(LogLevel.Info, "warmup-{0}", "value"), 128);
            ForceGarbageCollection();

            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();

            for (var iteration = 0; iteration < HotPathIterations; iteration++)
            {
                logger.LogFormat(LogLevel.Info, "message-{0}", iteration);
            }

            var allocatedAfter = GC.GetAllocatedBytesForCurrentThread();
            var allocatedPerCall = (allocatedAfter - allocatedBefore) / (double) HotPathIterations;

            Assert.That(target.LoggedCount, Is.EqualTo(HotPathIterations + 128));
            Assert.That(allocatedPerCall, Is.LessThan(1200d), $"Average allocated bytes per LogFormat call was {allocatedPerCall:F2}.");
        }

        [Test]
        public void DispatchingFlush_QueuedBurst_CompletesWithinReasonableBudget()
        {
            var target = new CountingOnlyLogTarget(isThreadSafe: false, dispatchToMainThread: true);
            var utilitySupplier = CreateUtilitySupplier();
            var syncContext = new QueuedSynchronizationContext();
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

            RunOnWorkerThread(() =>
            {
                for (var iteration = 0; iteration < QueuedLogCount; iteration++)
                {
                    ((ILogTarget) dispatchingTarget).Log(LogLevel.Info,
                                                         "PerfCategory",
                                                         "queued-message",
                                                         new LogAttributes(LogImportance.Important) { TimeUtc = DateTime.UtcNow },
                                                         null);
                }
            });

            Assert.That(syncContext.PendingCount, Is.EqualTo(QueuedLogCount));

            var stopwatch = Stopwatch.StartNew();
            syncContext.FlushPostedCallbacks();
            stopwatch.Stop();

            Assert.That(target.LoggedCount, Is.EqualTo(QueuedLogCount));
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(250L), $"Dispatch flush took {stopwatch.ElapsedMilliseconds} ms.");
        }

        [Test]
        public void BatchFlush_Burst_CompletesWithinReasonableBudget()
        {
            const int batchSize = 64;
            const int updatePeriodMs = 1000;

            var target = new CountingOnlyLogTarget();
            var startTimeUtc = DateTime.UtcNow;
            var batchingTarget = new LogTargetBatchLogsDecoration(
                new LogTargetBatchLogsConfiguration
                {
                    Enabled = true,
                    MaxCountLogs = batchSize,
                    UpdatePeriodMs = updatePeriodMs
                },
                target,
                startTimeUtc);

            for (var iteration = 0; iteration < QueuedLogCount; iteration++)
            {
                ((ILogTarget) batchingTarget).Log(LogLevel.Info,
                                                  "PerfCategory",
                                                  "batched-message",
                                                  new LogAttributes(LogImportance.Important)
                                                  {
                                                      TimeUtc = startTimeUtc.AddMilliseconds(iteration)
                                                  },
                                                  null);
            }

            var stopwatch = Stopwatch.StartNew();
            var currentTimeUtc = startTimeUtc;
            while (target.LoggedCount < QueuedLogCount)
            {
                currentTimeUtc = currentTimeUtc.AddMilliseconds(updatePeriodMs + 1);
                ((IScheduledUpdate) batchingTarget).Update(currentTimeUtc, updatePeriodMs + 1);
            }

            stopwatch.Stop();

            Assert.That(target.LoggedCount, Is.EqualTo(QueuedLogCount));
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(250L), $"Batch flush took {stopwatch.ElapsedMilliseconds} ms.");
        }

        private static CoreLogger CreateLogger(params ILogTarget[] logTargets)
        {
            return new CoreLogger("PerfCategory",
                                  string.Empty,
                                  logTargets,
                                  CreateUtilitySupplier(),
                                  512);
        }

        private static UtilitySupplier CreateUtilitySupplier()
        {
            return new UtilitySupplier(0,
                                       new StackTraceFormatter(Application.dataPath, new StackTraceFormatterConfiguration()));
        }

        private static void WarmUp(Action action, int iterations)
        {
            for (var iteration = 0; iteration < iterations; iteration++)
            {
                action();
            }
        }

        private static void ForceGarbageCollection()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private static void RunOnWorkerThread(Action action)
        {
            Exception exception = null;
            using var completed = new System.Threading.ManualResetEventSlim(false);
            var worker = new System.Threading.Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                finally
                {
                    completed.Set();
                }
            });

            worker.Start();
            Assert.That(completed.Wait(TimeSpan.FromSeconds(5)), Is.True, "Worker thread timed out.");
            if (exception != null)
            {
                throw exception;
            }
        }
    }
}
