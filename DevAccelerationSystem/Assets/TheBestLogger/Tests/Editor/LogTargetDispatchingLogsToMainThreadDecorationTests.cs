using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using TheBestLogger.Core.Utilities;
using UnityEngine;

namespace TheBestLogger.Tests.Editor
{
    [TestFixture]
    public class LogTargetDispatchingLogsToMainThreadDecorationTests
    {
        private ILogTarget _logDecoratedTarget;
        private MockLogTarget _mockLogTarget;
        private LogTargetDispatchingLogsToMainThreadConfiguration _config;
        private QueuedSynchronizationContext _synchronizationContext;
        private UtilitySupplier _utilitySupplier;

        [SetUp]
        public void SetUp()
        {
            _config = new LogTargetDispatchingLogsToMainThreadConfiguration
            {
                Enabled = true,
                SingleLogDispatchEnabled = true,
                BatchLogsDispatchEnabled = true
            };
            _mockLogTarget = new MockLogTarget();
            _synchronizationContext = new QueuedSynchronizationContext();
            _utilitySupplier = new UtilitySupplier(0,
                                                   new StackTraceFormatter(Application.dataPath, new StackTraceFormatterConfiguration()));
            _logDecoratedTarget = new LogTargetDispatchingLogsToMainThreadDecoration(_config,
                                                                                      _mockLogTarget,
                                                                                      _synchronizationContext,
                                                                                      _utilitySupplier);
        }

        [Test]
        public void Log_QueuesSingleLogUntilMainThreadFlush_WhenCalledOffMainThread()
        {
            RunOnWorkerThread(() =>
            {
                var logAttributes = new LogAttributes(LogImportance.Critical) { TimeUtc = DateTime.UtcNow };
                _logDecoratedTarget.Log(LogLevel.Error, "TestCategory", "Message to be dispatched", logAttributes, null);
            });

            Assert.That(_synchronizationContext.PendingCount, Is.EqualTo(1));
            Assert.That(_mockLogTarget.LoggedBatches.Count, Is.EqualTo(0));

            _synchronizationContext.FlushPostedCallbacks();

            Assert.That(_mockLogTarget.LoggedBatches.Count, Is.EqualTo(1));
            Assert.That(_mockLogTarget.LoggedBatches[0][0].Message, Is.EqualTo("Message to be dispatched"));
        }

        [Test]
        public void Log_DoesNotQueue_WhenCalledOnMainThread()
        {
            var logAttributes = new LogAttributes(LogImportance.Critical) { TimeUtc = DateTime.UtcNow };

            _logDecoratedTarget.Log(LogLevel.Error, "TestCategory", "Message without dispatch", logAttributes, null);

            Assert.That(_synchronizationContext.PendingCount, Is.EqualTo(0));
            Assert.That(_mockLogTarget.LoggedBatches.Count, Is.EqualTo(1));
            Assert.That(_mockLogTarget.LoggedBatches[0][0].Message, Is.EqualTo("Message without dispatch"));
        }

        [Test]
        public void LogBatch_QueuesBatchUntilMainThreadFlush_WhenCalledOffMainThread()
        {
            RunOnWorkerThread(() =>
            {
                var logBatch = new List<LogEntry>
                {
                    new(LogLevel.Info, "TestCategory", "Batch message 1", new LogAttributes(LogImportance.Important), null),
                    new(LogLevel.Info, "TestCategory", "Batch message 2", new LogAttributes(LogImportance.Important), null)
                };

                _logDecoratedTarget.LogBatch(logBatch);
            });

            Assert.That(_synchronizationContext.PendingCount, Is.EqualTo(1));
            Assert.That(_mockLogTarget.LoggedBatches.Count, Is.EqualTo(0));

            _synchronizationContext.FlushPostedCallbacks();

            Assert.That(_mockLogTarget.LoggedBatches.Count, Is.EqualTo(1));
            Assert.That(_mockLogTarget.LoggedBatches[0].Count, Is.EqualTo(2));
            Assert.That(_mockLogTarget.LoggedBatches[0][0].Message, Is.EqualTo("Batch message 1"));
            Assert.That(_mockLogTarget.LoggedBatches[0][1].Message, Is.EqualTo("Batch message 2"));
        }

        [Test]
        public void LogBatch_DoesNotQueueEmptyPayload_WhenCalledOffMainThread()
        {
            RunOnWorkerThread(() => { _logDecoratedTarget.LogBatch(Array.Empty<LogEntry>()); });

            Assert.That(_synchronizationContext.PendingCount, Is.EqualTo(0));
            Assert.That(_mockLogTarget.LoggedBatches.Count, Is.EqualTo(0));
        }

        [Test]
        public void ApplyConfiguration_UpdatesConfiguration()
        {
            var newConfig = new MockLogTargetConfiguration
            {
                DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration { Enabled = false }
            };

            _logDecoratedTarget.ApplyConfiguration(newConfig);

            Assert.IsFalse(_logDecoratedTarget.Configuration.DispatchingLogsToMainThread.Enabled);
        }

        [Test]
        public void Log_WhenMoreThanOneTickBudgetIsQueued_DrainsOnlyOneBudgetPerCallback()
        {
            RunOnWorkerThread(() =>
            {
                for (var index = 0; index < 100; index++)
                {
                    _logDecoratedTarget.Log(LogLevel.Info,
                                            "TestCategory",
                                            "Message " + index,
                                            new LogAttributes(LogImportance.Important),
                                            null);
                }
            });

            Assert.That(_synchronizationContext.PendingCount, Is.EqualTo(1));
            Assert.That(_synchronizationContext.FlushOnePostedCallback(), Is.True);
            Assert.That(_mockLogTarget.LoggedEntries.Count, Is.EqualTo(64));
            Assert.That(_synchronizationContext.PendingCount, Is.EqualTo(1));
        }

        [Test]
        public void LogBatch_WhenBatchExceedsTickBudget_SplitsWorkAcrossCallbacks()
        {
            RunOnWorkerThread(() =>
            {
                var logBatch = new List<LogEntry>();
                for (var index = 0; index < 100; index++)
                {
                    logBatch.Add(new LogEntry(LogLevel.Info,
                                              "TestCategory",
                                              "Message " + index,
                                              new LogAttributes(LogImportance.Important),
                                              null));
                }

                _logDecoratedTarget.LogBatch(logBatch);
            });

            Assert.That(_synchronizationContext.PendingCount, Is.EqualTo(1));
            Assert.That(_synchronizationContext.FlushOnePostedCallback(), Is.True);
            Assert.That(_mockLogTarget.LoggedEntries.Count, Is.EqualTo(64));
            Assert.That(_synchronizationContext.PendingCount, Is.EqualTo(1));
        }

        [Test]
        public void DispatchOverflow_WithFullBatchSegments_MakesPayloadProgress()
        {
            RunOnWorkerThread(() =>
            {
                var logBatch = new List<LogEntry>();
                for (var index = 0; index < 1088; index++)
                {
                    logBatch.Add(new LogEntry(LogLevel.Info,
                                              "TestCategory",
                                              "Message " + index,
                                              new LogAttributes(LogImportance.Important),
                                              null));
                }

                _logDecoratedTarget.LogBatch(logBatch);
            });

            Assert.That(_synchronizationContext.FlushOnePostedCallback(), Is.True);
            Assert.That(_mockLogTarget.LoggedEntries.Count, Is.EqualTo(65));
            Assert.That(_mockLogTarget.LoggedEntries[0].Category, Is.EqualTo("TheBestLogger"));
            Assert.That(_mockLogTarget.LoggedEntries[0].Message, Does.Contain("Dropped 64 pending"));
            Assert.That(_mockLogTarget.LoggedEntries[1].Category, Is.EqualTo("TestCategory"));
        }

        [Test]
        public void QueuedThrowingTarget_StopsAfterMuteThreshold()
        {
            var throwingTarget = new ThrowingLogTarget();
            var decoratedTarget = new LogTargetDispatchingLogsToMainThreadDecoration(_config,
                                                                                       throwingTarget,
                                                                                       _synchronizationContext,
                                                                                       _utilitySupplier);

            RunOnWorkerThread(() =>
            {
                for (var index = 0; index < 100; index++)
                {
                    ((ILogTarget) decoratedTarget).Log(LogLevel.Info,
                                                       "TestCategory",
                                                       "Message " + index,
                                                       new LogAttributes(LogImportance.Important),
                                                       null);
                }
            });

            _synchronizationContext.FlushPostedCallbacks();

            Assert.That(throwingTarget.LogCallCount, Is.EqualTo(3));
            Assert.That(throwingTarget.IsLogLevelAllowed(LogLevel.Info, "TestCategory"), Is.False);
            Assert.That(_synchronizationContext.PendingCount, Is.EqualTo(0));
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

        private sealed class ThrowingLogTarget : MockLogTarget
        {
            public int LogCallCount { get; private set; }

            public override void Log(LogLevel level,
                                     string category,
                                     string message,
                                     LogAttributes logAttributes,
                                     Exception exception)
            {
                LogCallCount++;
                throw new InvalidOperationException("Expected test failure.");
            }
        }
    }
}
