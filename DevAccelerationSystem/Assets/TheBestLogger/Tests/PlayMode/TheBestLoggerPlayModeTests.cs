using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using TheBestLogger.Core.Utilities;
using UnityEngine;
using UnityEngine.TestTools;

namespace TheBestLogger.Tests.PlayMode
{
    [TestFixture]
    public class TheBestLoggerPlayModeTests
    {
        [UnityTest]
        public IEnumerator CoreLogger_WritesRuntimeAttributesAcrossFrames()
        {
            var target = new PlayModeLogTarget();
            var utilitySupplier = CreateUtilitySupplier(0);
            var logger = new CoreLogger("RuntimeCategory", "HUD", new ILogTarget[] { target }, utilitySupplier, 256);

            utilitySupplier.TagsRegistry.AddTag("playmode");
            logger.LogInfo("frame-zero");
            var firstEntry = target.LoggedEntries[0];

            yield return null;

            utilitySupplier.TagsRegistry.AddTag("next-frame");
            logger.LogInfo("frame-one");
            var secondEntry = target.LoggedEntries[1];

            Assert.That(target.LoggedEntries.Count, Is.EqualTo(2));
            Assert.That(firstEntry.Message, Is.EqualTo("<HUD> frame-zero"));
            CollectionAssert.Contains(firstEntry.Attributes.Tags, "playmode");
            CollectionAssert.Contains(secondEntry.Attributes.Tags, "next-frame");
            Assert.That(secondEntry.Attributes.TimeUtc, Is.GreaterThan(firstEntry.Attributes.TimeUtc));
        }

        [UnityTest]
        public IEnumerator DispatchingDecoration_QueuesBackgroundLogsUntilMainThreadFlush()
        {
            var target = new PlayModeLogTarget();
            var syncContext = new PlayModeQueuedSynchronizationContext();
            var utilitySupplier = CreateUtilitySupplier(0);
            var decoratedTarget = new LogTargetDispatchingLogsToMainThreadDecoration(
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
                ((ILogTarget) decoratedTarget).Log(LogLevel.Error,
                                                   "RuntimeCategory",
                                                   "queued-from-worker",
                                                   new LogAttributes(LogImportance.Critical) { TimeUtc = DateTime.UtcNow },
                                                   null);
            });

            Assert.That(syncContext.PendingCount, Is.EqualTo(1));
            Assert.That(target.LoggedEntries.Count, Is.EqualTo(0));

            yield return null;

            syncContext.FlushPostedCallbacks();

            Assert.That(target.LoggedEntries.Count, Is.EqualTo(1));
            Assert.That(target.LoggedEntries[0].Message, Is.EqualTo("queued-from-worker"));
        }

        [UnityTest]
        public IEnumerator BatchDecoration_FlushesImportantLogsBeforeNiceToHaveLogs()
        {
            var target = new PlayModeLogTarget();
            var startTimeUtc = DateTime.UtcNow;
            var decoratedTarget = new LogTargetBatchLogsDecoration(
                new LogTargetBatchLogsConfiguration
                {
                    Enabled = true,
                    UpdatePeriodMs = 50,
                    MaxCountLogs = 10
                },
                target,
                startTimeUtc);

            ((ILogTarget) decoratedTarget).Log(LogLevel.Info,
                                               "RuntimeCategory",
                                               "nice",
                                               new LogAttributes(LogImportance.NiceToHave) { TimeUtc = startTimeUtc.AddMilliseconds(5) },
                                               null);
            ((ILogTarget) decoratedTarget).Log(LogLevel.Info,
                                               "RuntimeCategory",
                                               "important",
                                               new LogAttributes(LogImportance.Important) { TimeUtc = startTimeUtc.AddMilliseconds(10) },
                                               null);

            yield return null;

            ((IScheduledUpdate) decoratedTarget).Update(startTimeUtc.AddMilliseconds(75), 75);

            Assert.That(target.LoggedBatches.Count, Is.EqualTo(1));
            Assert.That(target.LoggedBatches[0].Count, Is.EqualTo(2));
            Assert.That(target.LoggedBatches[0][0].Message, Is.EqualTo("important"));
            Assert.That(target.LoggedBatches[0][1].Message, Is.EqualTo("nice"));
        }

        [UnityTest]
        public IEnumerator CoreLogger_LongRunningSessionAcrossFrames_PreservesAllEntriesAndAttributes()
        {
            const int frameCount = 12;
            const int logsPerFrame = 15;

            var target = new PlayModeLogTarget();
            var utilitySupplier = CreateUtilitySupplier(0);
            var logger = new CoreLogger("RuntimeCategory", "HUD", new ILogTarget[] { target }, utilitySupplier, 256);

            for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                utilitySupplier.TagsRegistry.AddTag($"frame-{frameIndex}");

                for (var logIndex = 0; logIndex < logsPerFrame; logIndex++)
                {
                    logger.LogInfo($"log-{frameIndex}-{logIndex}");
                }

                yield return null;
            }

            var expectedCount = frameCount * logsPerFrame;
            Assert.That(target.LoggedEntries.Count, Is.EqualTo(expectedCount));
            Assert.That(target.LoggedEntries.Select(entry => entry.Message).Distinct().Count(), Is.EqualTo(expectedCount));

            foreach (var entry in target.LoggedEntries)
            {
                Assert.That(entry.Attributes, Is.Not.Null);
                Assert.That(entry.Attributes.Tags, Is.Not.Null);
                Assert.That(entry.Attributes.TimeUtc, Is.GreaterThan(DateTime.MinValue));
            }
        }

        [UnityTest]
        public IEnumerator DispatchingDecoration_BurstAcrossFrames_FlushesQueuedLogsExactlyOnce()
        {
            const int frameCount = 8;
            const int workerCount = 4;
            const int logsPerWorkerPerFrame = 10;

            var target = new PlayModeLogTarget();
            var syncContext = new PlayModeQueuedSynchronizationContext();
            var utilitySupplier = CreateUtilitySupplier(0);
            var decoratedTarget = new LogTargetDispatchingLogsToMainThreadDecoration(
                new LogTargetDispatchingLogsToMainThreadConfiguration
                {
                    Enabled = true,
                    SingleLogDispatchEnabled = true,
                    BatchLogsDispatchEnabled = true
                },
                target,
                syncContext,
                utilitySupplier);

            for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                var exceptions = RunConcurrentBurst(workerCount,
                                                    logsPerWorkerPerFrame,
                                                    (workerId, logIndex) =>
                                                    {
                                                        ((ILogTarget) decoratedTarget).Log(LogLevel.Info,
                                                                                           "RuntimeCategory",
                                                                                           $"queued-{frameIndex}-{workerId}-{logIndex}",
                                                                                           new LogAttributes(LogImportance.Important)
                                                                                           {
                                                                                               TimeUtc = DateTime.UtcNow
                                                                                           },
                                                                                           null);
                                                    });

                Assert.That(exceptions, Is.Empty);
                Assert.That(syncContext.PendingCount, Is.EqualTo(1));

                yield return null;

                syncContext.FlushPostedCallbacks();
            }

            var expectedCount = frameCount * workerCount * logsPerWorkerPerFrame;
            Assert.That(target.LoggedEntries.Count, Is.EqualTo(expectedCount));
            Assert.That(target.LoggedEntries.Select(entry => entry.Message).Distinct().Count(), Is.EqualTo(expectedCount));
        }

        [UnityTest]
        public IEnumerator BatchAndDispatch_LongRunningMixedImportancePressure_FlushesAllLogsExactlyOnce()
        {
            const int frameCount = 10;
            const int workerCount = 4;
            const int logsPerWorkerPerFrame = 12;
            const int updatePeriodMs = 30;
            const int batchSize = 16;

            var target = new PlayModeLogTarget(isThreadSafe: false, dispatchToMainThread: true);
            var syncContext = new PlayModeQueuedSynchronizationContext();
            var utilitySupplier = CreateUtilitySupplier(0);
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
            var startTimeUtc = DateTime.UtcNow;
            var batchingTarget = new LogTargetBatchLogsDecoration(
                new LogTargetBatchLogsConfiguration
                {
                    Enabled = true,
                    UpdatePeriodMs = updatePeriodMs,
                    MaxCountLogs = batchSize
                },
                dispatchingTarget,
                startTimeUtc);
            var logger = new CoreLogger("RuntimeCategory",
                                        string.Empty,
                                        new ILogTarget[] { batchingTarget },
                                        utilitySupplier,
                                        512);

            var expectedMessages = new HashSet<string>();
            var currentTimeUtc = startTimeUtc;

            for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                var frameCopy = frameIndex;
                var exceptions = RunConcurrentBurst(workerCount,
                                                    logsPerWorkerPerFrame,
                                                    (workerId, logIndex) =>
                                                    {
                                                        var message = $"mixed-{frameCopy}-{workerId}-{logIndex}";
                                                        var importance = logIndex % 6 == 0
                                                                             ? LogImportance.Critical
                                                                             : logIndex % 2 == 0
                                                                                 ? LogImportance.Important
                                                                                 : LogImportance.NiceToHave;
                                                        lock (expectedMessages)
                                                        {
                                                            expectedMessages.Add(message);
                                                        }

                                                        logger.LogInfo(message, new LogAttributes(importance));
                                                    });

                Assert.That(exceptions, Is.Empty);

                yield return null;

                currentTimeUtc = currentTimeUtc.AddMilliseconds(updatePeriodMs + 5);
                ((IScheduledUpdate) batchingTarget).Update(currentTimeUtc, updatePeriodMs + 5);
                syncContext.FlushPostedCallbacks();
            }

            for (var flushIndex = 0; flushIndex < 6; flushIndex++)
            {
                yield return null;
                currentTimeUtc = currentTimeUtc.AddMilliseconds(updatePeriodMs + 5);
                ((IScheduledUpdate) batchingTarget).Update(currentTimeUtc, updatePeriodMs + 5);
                syncContext.FlushPostedCallbacks();
            }

            Assert.That(target.LoggedEntries.Count, Is.EqualTo(expectedMessages.Count));
            Assert.That(target.LoggedEntries.Select(entry => entry.Message).Distinct().Count(), Is.EqualTo(expectedMessages.Count));
        }

        private static UtilitySupplier CreateUtilitySupplier(uint minTimestampPeriodMs)
        {
            return new UtilitySupplier(minTimestampPeriodMs,
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
                                      for (var logIndex = 0; logIndex < logsPerWorker; logIndex++)
                                      {
                                          try
                                          {
                                              action(workerId, logIndex);
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

        private sealed class PlayModeQueuedSynchronizationContext : SynchronizationContext
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

        private sealed class PlayModeLogTarget : LogTarget
        {
            public List<List<LogEntry>> LoggedBatches { get; } = new();
            public List<LogEntry> LoggedEntries { get; } = new();

            public PlayModeLogTarget(bool isThreadSafe = true, bool dispatchToMainThread = false)
            {
                ApplyConfiguration(new PlayModeLogTargetConfiguration
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

            public override string LogTargetConfigurationName => nameof(PlayModeLogTargetConfiguration);

            public override void Log(LogLevel level,
                                     string category,
                                     string message,
                                     LogAttributes logAttributes,
                                     Exception exception)
            {
                LogBatch(new[] { new LogEntry(level, category, message, logAttributes, exception) });
            }

            public override void LogBatch(IReadOnlyList<LogEntry> logBatch)
            {
                var batchCopy = new List<LogEntry>(logBatch);
                LoggedBatches.Add(batchCopy);
                LoggedEntries.AddRange(batchCopy);
            }
        }

        private sealed class PlayModeLogTargetConfiguration : LogTargetConfiguration
        {
        }
    }
}
