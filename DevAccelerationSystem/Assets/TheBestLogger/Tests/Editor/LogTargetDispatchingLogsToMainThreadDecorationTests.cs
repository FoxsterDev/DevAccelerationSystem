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
