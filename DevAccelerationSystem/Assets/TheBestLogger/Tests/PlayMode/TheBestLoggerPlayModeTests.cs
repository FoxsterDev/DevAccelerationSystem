using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
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

            public PlayModeLogTarget()
            {
                ApplyConfiguration(new PlayModeLogTargetConfiguration
                {
                    MinLogLevel = LogLevel.Debug,
                    IsThreadSafe = true,
                    DebugMode = new DebugModeConfiguration(),
                    BatchLogs = new LogTargetBatchLogsConfiguration(),
                    DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
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
