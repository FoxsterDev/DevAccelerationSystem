using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using TheBestLogger.Core.Utilities;
using UnityEngine;
using UnityEngine.TestTools;

namespace TheBestLogger.Tests.Editor
{
    [TestFixture]
    public class SafeThirdPartyLogTargetTests
    {
        [SetUp]
        public void SetUp()
        {
            var field = typeof(SafeThirdPartyLogTarget).GetField("_successfullyInit", BindingFlags.Static | BindingFlags.NonPublic);
            field?.SetValue(null, null);
        }

        [Test]
        public void Log_WhenThirdPartyThrows_MutesTargetAndSkipsSubsequentCalls()
        {
            var target = new ThrowingSafeThirdPartyLogTarget(throwOnCallNumber: 1);
            LogAssert.Expect(LogType.Exception, "InvalidOperationException: Simulated third-party failure.");

            Assert.DoesNotThrow(() => target.Log(LogLevel.Error, "Gameplay", "first", new LogAttributes(), null));
            Assert.DoesNotThrow(() => target.Log(LogLevel.Error, "Gameplay", "second", new LogAttributes(), null));

            Assert.That(target.CallCount, Is.EqualTo(1));
            Assert.That(target.IsLogLevelAllowed(LogLevel.Error, "Gameplay"), Is.False);
        }

        [Test]
        public void LogBatch_WhenThirdPartyThrows_DoesNotPropagateAndStopsFurtherCalls()
        {
            var target = new ThrowingSafeThirdPartyLogTarget(throwOnCallNumber: 1);
            var batch = new List<LogEntry>
            {
                CreateLogEntry("first"),
                CreateLogEntry("second"),
                CreateLogEntry("third")
            };
            LogAssert.Expect(LogType.Exception, "InvalidOperationException: Simulated third-party failure.");

            Assert.DoesNotThrow(() => target.LogBatch(batch));

            Assert.That(target.CallCount, Is.EqualTo(1));
            Assert.That(target.IsLogLevelAllowed(LogLevel.Info, "Gameplay"), Is.False);
        }

        [Test]
        public void CoreLogger_WhenSafeThirdPartyTargetFails_HealthyTargetStillReceivesCurrentAndFutureLogs()
        {
            var failingTarget = new ThrowingSafeThirdPartyLogTarget(throwOnCallNumber: 1);
            var healthyTarget = new MockLogTarget();
            var logger = CreateLogger(failingTarget, healthyTarget);
            LogAssert.Expect(LogType.Exception, "InvalidOperationException: Simulated third-party failure.");

            Assert.DoesNotThrow(() => logger.LogInfo("first"));
            Assert.DoesNotThrow(() => logger.LogInfo("second"));

            Assert.That(failingTarget.CallCount, Is.EqualTo(1));
            Assert.That(healthyTarget.LoggedEntries.Count, Is.EqualTo(2));
            Assert.That(healthyTarget.LoggedEntries[0].Message, Is.EqualTo("first"));
            Assert.That(healthyTarget.LoggedEntries[1].Message, Is.EqualTo("second"));
        }

        [Test]
        public void DispatchingAndBatching_WhenWrappedSafeThirdPartyTargetFails_HealthyTargetKeepsFlushing()
        {
            var failingInnerTarget = new ThrowingSafeThirdPartyLogTarget(throwOnCallNumber: 1, isThreadSafe: false, dispatchToMainThread: true);
            var healthyTarget = new ConcurrentCaptureLogTarget(isThreadSafe: false, dispatchToMainThread: true);
            var syncContext = new QueuedSynchronizationContext();
            var utilitySupplier = CreateUtilitySupplier();
            var currentTime = DateTime.UtcNow;
            LogAssert.Expect(LogType.Exception, "InvalidOperationException: Simulated third-party failure.");

            var failingTarget = new LogTargetBatchLogsDecoration(
                new LogTargetBatchLogsConfiguration
                {
                    Enabled = true,
                    MaxCountLogs = 8,
                    UpdatePeriodMs = 100
                },
                new LogTargetDispatchingLogsToMainThreadDecoration(
                    new LogTargetDispatchingLogsToMainThreadConfiguration
                    {
                        Enabled = true,
                        SingleLogDispatchEnabled = true,
                        BatchLogsDispatchEnabled = true
                    },
                    failingInnerTarget,
                    syncContext,
                    utilitySupplier),
                currentTime);
            var healthyDispatchingTarget = new LogTargetDispatchingLogsToMainThreadDecoration(
                new LogTargetDispatchingLogsToMainThreadConfiguration
                {
                    Enabled = true,
                    SingleLogDispatchEnabled = true,
                    BatchLogsDispatchEnabled = true
                },
                healthyTarget,
                syncContext,
                utilitySupplier);

            var logger = new CoreLogger("Gameplay",
                                        string.Empty,
                                        new ILogTarget[] { failingTarget, healthyDispatchingTarget },
                                        utilitySupplier,
                                        512);

            RunOnWorkerThread(() =>
            {
                logger.LogInfo("first", new LogAttributes(LogImportance.Important));
                logger.LogInfo("second", new LogAttributes(LogImportance.Important));
            });

            currentTime = currentTime.AddMilliseconds(150);
            ((IScheduledUpdate) failingTarget).Update(currentTime, 150);
            syncContext.FlushPostedCallbacks();

            Assert.That(failingInnerTarget.CallCount, Is.EqualTo(1));
            Assert.That(healthyTarget.LoggedCount, Is.EqualTo(2));
        }

        private static CoreLogger CreateLogger(params ILogTarget[] targets)
        {
            return new CoreLogger("Gameplay",
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

        private static LogEntry CreateLogEntry(string message)
        {
            return new LogEntry(LogLevel.Info,
                                "Gameplay",
                                message,
                                new LogAttributes { TimeUtc = DateTime.UtcNow, TimeStampFormatted = DateTime.UtcNow.ToString("O") },
                                null);
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

        private sealed class ThrowingSafeThirdPartyLogTarget : SafeThirdPartyLogTarget
        {
            private readonly int _throwOnCallNumber;
            private int _callCount;

            public int CallCount => _callCount;

            public ThrowingSafeThirdPartyLogTarget(int throwOnCallNumber,
                                                   bool isThreadSafe = true,
                                                   bool dispatchToMainThread = false)
            {
                _throwOnCallNumber = throwOnCallNumber;
                ApplyConfiguration(new MockLogTargetConfiguration
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

            public override string LogTargetConfigurationName => nameof(MockLogTargetConfiguration);

            protected override bool IsThirdPartyReady => true;

            protected override void TryCreateThirdPartyLogMethodDelegate()
            {
            }

            public override void CallThirdPartyLogMethod(LogLevel level,
                                                         string category,
                                                         string message,
                                                         LogAttributes logAttributes,
                                                         Exception exception = null)
            {
                var newCount = Interlocked.Increment(ref _callCount);
                if (newCount == _throwOnCallNumber)
                {
                    throw new InvalidOperationException("Simulated third-party failure.");
                }
            }
        }
    }
}
