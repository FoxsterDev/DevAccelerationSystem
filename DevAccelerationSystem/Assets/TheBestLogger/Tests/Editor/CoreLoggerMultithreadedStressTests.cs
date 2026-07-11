using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using TheBestLogger.Core.Utilities;
using UnityEngine;

namespace TheBestLogger.Tests.Editor
{
    [TestFixture]
    public class CoreLoggerMultithreadedStressTests
    {
        [Test]
        public void ThreadSafeTarget_ConcurrentBurst_DeliversAllLogsWithoutExceptions()
        {
            const int workerCount = 16;
            const int logsPerWorker = 200;

            var target = new ConcurrentCaptureLogTarget();
            var logger = CreateLogger(target);
            var exceptions = RunConcurrentBurst(workerCount,
                                                logsPerWorker,
                                                (workerId, iteration) => logger.LogInfo($"worker-{workerId}-log-{iteration}"));

            Assert.That(exceptions, Is.Empty);
            Assert.That(target.LoggedCount, Is.EqualTo(workerCount * logsPerWorker));

            var uniqueMessagesCount = target.LoggedEntries
                                            .Select(entry => entry.Message)
                                            .Distinct()
                                            .Count();
            Assert.That(uniqueMessagesCount, Is.EqualTo(workerCount * logsPerWorker));
        }

        [Test]
        public void ThreadSafeTarget_64ThreadBurst_DeliversAllLogsWithoutExceptions()
        {
            const int workerCount = 64;
            const int logsPerWorker = 50;

            var target = new ConcurrentCaptureLogTarget();
            var logger = CreateLogger(target);
            var exceptions = RunConcurrentBurst(workerCount,
                                                logsPerWorker,
                                                (workerId, iteration) => logger.LogInfo($"sixtyfour-{workerId}-{iteration}"));

            Assert.That(exceptions, Is.Empty);
            Assert.That(target.LoggedCount, Is.EqualTo(workerCount * logsPerWorker));
        }

        [Test]
        public void DispatchingTarget_ConcurrentBurst_BoundsQueueAndFlushesRetainedLogs()
        {
            const int workerCount = 8;
            const int logsPerWorker = 150;

            var originalTarget = new ConcurrentCaptureLogTarget(isThreadSafe: false, dispatchToMainThread: true);
            var synchronizationContext = new QueuedSynchronizationContext();
            var utilitySupplier = CreateUtilitySupplier();
            var dispatchingTarget = new LogTargetDispatchingLogsToMainThreadDecoration(
                new LogTargetDispatchingLogsToMainThreadConfiguration
                {
                    Enabled = true,
                    SingleLogDispatchEnabled = true,
                    BatchLogsDispatchEnabled = true
                },
                originalTarget,
                synchronizationContext,
                utilitySupplier);

            var logger = new CoreLogger("Gameplay",
                                        string.Empty,
                                        new ILogTarget[] { dispatchingTarget },
                                        utilitySupplier,
                                        1024);

            var exceptions = RunConcurrentBurst(workerCount,
                                                logsPerWorker,
                                                (workerId, iteration) => logger.LogInfo($"dispatch-{workerId}-{iteration}"));

            Assert.That(exceptions, Is.Empty);
            Assert.That(synchronizationContext.PendingCount, Is.EqualTo(1));
            Assert.That(originalTarget.LoggedCount, Is.EqualTo(0));

            synchronizationContext.FlushPostedCallbacks();

            Assert.That(originalTarget.LoggedCount, Is.EqualTo(1025));
            Assert.That(originalTarget.LoggedEntries.Count(entry => entry.Category == "TheBestLogger"), Is.EqualTo(1));
        }

        [Test]
        public void NonThreadSafeTarget_ConcurrentBurstWithoutDispatch_SkipsOffMainThreadLogsWithoutExceptions()
        {
            const int workerCount = 12;
            const int logsPerWorker = 120;

            var target = new ConcurrentCaptureLogTarget(isThreadSafe: false, dispatchToMainThread: false);
            var logger = CreateLogger(target);

            var exceptions = RunConcurrentBurst(workerCount,
                                                logsPerWorker,
                                                (workerId, iteration) => logger.LogInfo($"skipped-{workerId}-{iteration}"));

            Assert.That(exceptions, Is.Empty);
            Assert.That(target.LoggedCount, Is.EqualTo(0));
        }

        [Test]
        public void GlobalTags_ConcurrentMutationWhileLogging_KeepsTagSnapshotsValid()
        {
            const int loggingWorkerCount = 8;
            const int mutatorWorkerCount = 4;
            const int logsPerWorker = 150;
            var candidateTags = Enumerable.Range(0, 12).Select(index => $"tag-{index}").ToArray();

            var target = new ConcurrentCaptureLogTarget();
            var utilitySupplier = CreateUtilitySupplier();
            var logger = new CoreLogger("Gameplay",
                                        string.Empty,
                                        new ILogTarget[] { target },
                                        utilitySupplier,
                                        1024);
            var exceptions = new ConcurrentQueue<Exception>();
            using var startSignal = new ManualResetEventSlim(false);

            var loggingTasks = Enumerable.Range(0, loggingWorkerCount)
                                         .Select(workerId => Task.Run(() =>
                                         {
                                             startSignal.Wait();
                                             for (var iteration = 0; iteration < logsPerWorker; iteration++)
                                             {
                                                 try
                                                 {
                                                     logger.LogInfo($"tags-{workerId}-{iteration}");
                                                 }
                                                 catch (Exception exception)
                                                 {
                                                     exceptions.Enqueue(exception);
                                                     break;
                                                 }
                                             }
                                         }))
                                         .ToArray();

            var mutatorTasks = Enumerable.Range(0, mutatorWorkerCount)
                                         .Select(workerId => Task.Run(() =>
                                         {
                                             startSignal.Wait();
                                             for (var iteration = 0; iteration < logsPerWorker; iteration++)
                                             {
                                                 try
                                                 {
                                                     var tag = candidateTags[(workerId + iteration) % candidateTags.Length];
                                                     utilitySupplier.TagsRegistry.AddTag(tag);
                                                     if ((iteration & 1) == 0)
                                                     {
                                                         utilitySupplier.TagsRegistry.RemoveTag(tag);
                                                     }
                                                 }
                                                 catch (Exception exception)
                                                 {
                                                     exceptions.Enqueue(exception);
                                                     break;
                                                 }
                                             }
                                         }))
                                         .ToArray();

            startSignal.Set();

            var allTasks = loggingTasks.Concat(mutatorTasks).ToArray();
            Assert.That(Task.WaitAll(allTasks, TimeSpan.FromSeconds(10)), Is.True, "Concurrent tags workload did not finish in time.");
            Assert.That(exceptions, Is.Empty);
            Assert.That(target.LoggedCount, Is.EqualTo(loggingWorkerCount * logsPerWorker));

            foreach (var entry in target.LoggedEntries)
            {
                Assert.That(entry.Attributes, Is.Not.Null);
                Assert.That(entry.Attributes.Tags, Is.Not.Null);
                Assert.That(entry.Attributes.Tags.Any(tag => tag == null), Is.False);
            }
        }

        [Test]
        public void BatchAndDispatch_UnderConcurrentPressure_FlushesAllLogsAcrossCycles()
        {
            const int workerCount = 8;
            const int logsPerWorker = 80;
            const int batchSize = 25;
            const int updatePeriodMs = 1000;

            var originalTarget = new ConcurrentCaptureLogTarget(isThreadSafe: false, dispatchToMainThread: true);
            var synchronizationContext = new QueuedSynchronizationContext();
            var utilitySupplier = CreateUtilitySupplier();
            var dispatchingTarget = new LogTargetDispatchingLogsToMainThreadDecoration(
                new LogTargetDispatchingLogsToMainThreadConfiguration
                {
                    Enabled = true,
                    SingleLogDispatchEnabled = true,
                    BatchLogsDispatchEnabled = true
                },
                originalTarget,
                synchronizationContext,
                utilitySupplier);
            var batchingTarget = new LogTargetBatchLogsDecoration(
                new LogTargetBatchLogsConfiguration
                {
                    Enabled = true,
                    MaxCountLogs = batchSize,
                    UpdatePeriodMs = updatePeriodMs
                },
                dispatchingTarget,
                DateTime.UtcNow);
            var logger = new CoreLogger("Gameplay",
                                        string.Empty,
                                        new ILogTarget[] { batchingTarget },
                                        utilitySupplier,
                                        1024);

            var exceptions = RunConcurrentBurst(workerCount,
                                                logsPerWorker,
                                                (workerId, iteration) =>
                                                {
                                                    var importance = iteration % 10 == 0
                                                                         ? LogImportance.Critical
                                                                         : iteration % 2 == 0
                                                                             ? LogImportance.Important
                                                                             : LogImportance.NiceToHave;
                                                    logger.LogInfo($"batch-dispatch-{workerId}-{iteration}", new LogAttributes(importance));
                                                });

            Assert.That(exceptions, Is.Empty);

            var totalLogs = workerCount * logsPerWorker;
            var criticalLogs = workerCount * (logsPerWorker / 10);
            if (logsPerWorker % 10 != 0)
            {
                criticalLogs += workerCount;
            }

            var queuedNonCriticalLogs = totalLogs - criticalLogs;
            var flushIterations = (queuedNonCriticalLogs + batchSize - 1) / batchSize + 2;
            var currentTime = DateTime.UtcNow;
            for (var iteration = 0; iteration < flushIterations; iteration++)
            {
                currentTime = currentTime.AddMilliseconds(updatePeriodMs + 1);
                ((IScheduledUpdate) batchingTarget).Update(currentTime, updatePeriodMs + 1);
                synchronizationContext.FlushPostedCallbacks();
            }

            synchronizationContext.FlushPostedCallbacks();

            Assert.That(originalTarget.LoggedCount, Is.EqualTo(totalLogs));
        }

        [Test]
        public void ThreadSafeTarget_DisposeDuringBurst_DoesNotThrowFromWorkerThreads()
        {
            const int workerCount = 8;
            const int maxLogsPerWorker = 2000;

            var target = new ConcurrentCaptureLogTarget();
            var logger = CreateLogger(target);
            var exceptions = new ConcurrentQueue<Exception>();
            using var started = new CountdownEvent(workerCount);
            using var stopSignal = new CancellationTokenSource();

            var tasks = Enumerable.Range(0, workerCount)
                                  .Select(workerId => Task.Run(() =>
                                  {
                                      started.Signal();
                                      try
                                      {
                                          for (var iteration = 0;
                                               iteration < maxLogsPerWorker && !stopSignal.IsCancellationRequested;
                                               iteration++)
                                          {
                                              logger.LogInfo($"dispose-{workerId}-{iteration}");
                                          }
                                      }
                                      catch (Exception exception)
                                      {
                                          exceptions.Enqueue(exception);
                                      }
                                  }))
                                  .ToArray();

            Assert.That(started.Wait(TimeSpan.FromSeconds(5)), Is.True);

            Thread.Sleep(25);
            Assert.DoesNotThrow(logger.Dispose);

            stopSignal.Cancel();

            Assert.That(Task.WaitAll(tasks, TimeSpan.FromSeconds(10)), Is.True, "Workers did not finish in time.");
            Assert.That(exceptions, Is.Empty);
            Assert.DoesNotThrow(() => logger.LogInfo("after-dispose"));
            Assert.That(target.LoggedCount, Is.GreaterThan(0));
        }

        private static CoreLogger CreateLogger(params ILogTarget[] logTargets)
        {
            return new CoreLogger("Gameplay",
                                  string.Empty,
                                  logTargets,
                                  CreateUtilitySupplier(),
                                  1024);
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

            var tasks = Enumerable.Range(0, workerCount)
                                  .Select(workerId => Task.Run(() =>
                                  {
                                      startSignal.Wait();
                                      for (var iteration = 0; iteration < logsPerWorker; iteration++)
                                      {
                                          try
                                          {
                                              action(workerId, iteration);
                                          }
                                          catch (Exception exception)
                                          {
                                              exceptions.Enqueue(exception);
                                              break;
                                          }
                                      }
                                  }))
                                  .ToArray();

            startSignal.Set();

            Assert.That(Task.WaitAll(tasks, TimeSpan.FromSeconds(10)), Is.True, "Workers did not finish in time.");
            return exceptions;
        }
    }
}
