using System;
using System.Threading;
using NUnit.Framework;
using TheBestLogger.Core.Utilities;
using UnityEngine;

namespace TheBestLogger.Tests.Editor
{
    [TestFixture]
    public class LogTargetBatchDispatchIntegrationTests
    {
        [Test]
        public void CriticalLogs_KeepSeparateSnapshotsAcrossQueuedDispatches()
        {
            var originalTarget = new MockLogTarget();
            var syncContext = new QueuedSynchronizationContext();
            var utilitySupplier = CreateUtilitySupplier();

            var dispatchingTarget = new LogTargetDispatchingLogsToMainThreadDecoration(
                new LogTargetDispatchingLogsToMainThreadConfiguration
                {
                    Enabled = true,
                    SingleLogDispatchEnabled = true,
                    BatchLogsDispatchEnabled = true
                },
                originalTarget,
                syncContext,
                utilitySupplier);

            var batchingTarget = new LogTargetBatchLogsDecoration(
                new LogTargetBatchLogsConfiguration
                {
                    Enabled = true,
                    MaxCountLogs = 10,
                    UpdatePeriodMs = 1000
                },
                dispatchingTarget,
                DateTime.UtcNow);

            RunOnWorkerThread(() =>
            {
                ((ILogTarget) batchingTarget).Log(LogLevel.Error,
                                                  "Gameplay",
                                                  "first",
                                                  new LogAttributes(LogImportance.Critical) { TimeUtc = DateTime.UtcNow },
                                                  null);
                ((ILogTarget) batchingTarget).Log(LogLevel.Error,
                                                  "Gameplay",
                                                  "second",
                                                  new LogAttributes(LogImportance.Critical) { TimeUtc = DateTime.UtcNow.AddMilliseconds(1) },
                                                  null);
            });

            Assert.That(syncContext.PendingCount, Is.EqualTo(2));
            Assert.That(originalTarget.LoggedBatches.Count, Is.EqualTo(0));

            syncContext.FlushPostedCallbacks();

            Assert.That(originalTarget.LoggedBatches.Count, Is.EqualTo(2));
            Assert.That(originalTarget.LoggedBatches[0][0].Message, Is.EqualTo("first"));
            Assert.That(originalTarget.LoggedBatches[1][0].Message, Is.EqualTo("second"));
        }

        [Test]
        public void ScheduledFlushes_KeepSeparateSnapshotsAcrossQueuedDispatches()
        {
            var originalTarget = new MockLogTarget();
            var syncContext = new QueuedSynchronizationContext();
            var utilitySupplier = CreateUtilitySupplier();
            var startTimeUtc = DateTime.UtcNow;

            var dispatchingTarget = new LogTargetDispatchingLogsToMainThreadDecoration(
                new LogTargetDispatchingLogsToMainThreadConfiguration
                {
                    Enabled = true,
                    SingleLogDispatchEnabled = true,
                    BatchLogsDispatchEnabled = true
                },
                originalTarget,
                syncContext,
                utilitySupplier);

            var batchingTarget = new LogTargetBatchLogsDecoration(
                new LogTargetBatchLogsConfiguration
                {
                    Enabled = true,
                    MaxCountLogs = 10,
                    UpdatePeriodMs = 1000
                },
                dispatchingTarget,
                startTimeUtc);

            RunOnWorkerThread(() =>
            {
                ((ILogTarget) batchingTarget).Log(LogLevel.Info,
                                                  "Gameplay",
                                                  "first-flush",
                                                  new LogAttributes(LogImportance.Important) { TimeUtc = startTimeUtc.AddMilliseconds(100) },
                                                  null);
                ((IScheduledUpdate) batchingTarget).Update(startTimeUtc.AddMilliseconds(1200), 1200);

                ((ILogTarget) batchingTarget).Log(LogLevel.Info,
                                                  "Gameplay",
                                                  "second-flush",
                                                  new LogAttributes(LogImportance.Important) { TimeUtc = startTimeUtc.AddMilliseconds(1300) },
                                                  null);
                ((IScheduledUpdate) batchingTarget).Update(startTimeUtc.AddMilliseconds(2400), 1200);
            });

            Assert.That(syncContext.PendingCount, Is.EqualTo(2));

            syncContext.FlushPostedCallbacks();

            Assert.That(originalTarget.LoggedBatches.Count, Is.EqualTo(2));
            Assert.That(originalTarget.LoggedBatches[0][0].Message, Is.EqualTo("first-flush"));
            Assert.That(originalTarget.LoggedBatches[1][0].Message, Is.EqualTo("second-flush"));
        }

        private static UtilitySupplier CreateUtilitySupplier()
        {
            return new UtilitySupplier(0,
                                       new StackTraceFormatter(Application.dataPath, new StackTraceFormatterConfiguration()));
        }

        private static void RunOnWorkerThread(ThreadStart action)
        {
            Exception exception = null;
            using var completed = new ManualResetEventSlim(false);
            var worker = new Thread(() =>
            {
                try
                {
                    action.Invoke();
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
