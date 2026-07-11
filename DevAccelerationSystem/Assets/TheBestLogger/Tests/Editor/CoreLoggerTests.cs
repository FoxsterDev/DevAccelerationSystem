using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using TheBestLogger.Core.Utilities;
using UnityEngine;

namespace TheBestLogger.Tests.Editor
{
    [TestFixture]
    public class CoreLoggerTests
    {
        private MockLogTarget _logTarget;
        private UtilitySupplier _utilitySupplier;
        private CoreLogger _logger;

        [SetUp]
        public void SetUp()
        {
            _logTarget = new MockLogTarget();
            _utilitySupplier = CreateUtilitySupplier(0);
            _logger = CreateLogger(_logTarget, _utilitySupplier, "Session", 256);
        }

        [Test]
        public void LogFormat_WithTwoGenericArguments_ForwardsBothArguments()
        {
            _logger.LogFormat(LogLevel.Info, "{0}-{1}", 7, "alpha");

            Assert.That(_logTarget.LoggedEntries.Count, Is.EqualTo(1));
            Assert.That(_logTarget.LoggedEntries[0].Category, Is.EqualTo("Gameplay"));
            Assert.That(_logTarget.LoggedEntries[0].Message, Is.EqualTo("<Session> 7-alpha"));
        }

        [Test]
        public void LogFormat_ThroughILogger_WithLogLevelAndMessage_ForwardsMessage()
        {
            ILogger logger = _logger;

            logger.LogFormat(LogLevel.Info, "plain-message");

            Assert.That(_logTarget.LoggedEntries.Count, Is.EqualTo(1));
            Assert.That(_logTarget.LoggedEntries[0].Category, Is.EqualTo("Gameplay"));
            Assert.That(_logTarget.LoggedEntries[0].Message, Is.EqualTo("<Session> plain-message"));
        }

        [Test]
        public void LogFormat_WithThreeGenericArguments_ForwardsAllArguments()
        {
            _logger.LogFormat(LogLevel.Warning, "{0}-{1}-{2}", 7, "alpha", true);

            Assert.That(_logTarget.LoggedEntries.Count, Is.EqualTo(1));
            Assert.That(_logTarget.LoggedEntries[0].Message, Is.EqualTo("<Session> 7-alpha-True"));
        }

        [Test]
        public void LogInfo_AddsTimestampTagsAndProvidedProperties()
        {
            _utilitySupplier.TagsRegistry.AddTag("prod");
            _utilitySupplier.TagsRegistry.AddTag("critical-path");

            var attrs = new LogAttributes("userId", "42");
            _logger.LogInfo("hello", attrs);

            var entry = _logTarget.LoggedEntries.Single();
            Assert.That(entry.Attributes.TimeStampFormatted, Is.Not.Empty);
            Assert.That(entry.Attributes.TimeUtc, Is.Not.EqualTo(default(DateTime)));
            CollectionAssert.Contains(entry.Attributes.Tags, "prod");
            CollectionAssert.Contains(entry.Attributes.Tags, "critical-path");
            Assert.That(entry.Attributes.Props.Any(p => p.Key == "userId" && (string) p.Value == "42"), Is.True);
        }

        [Test]
        public void LogInfo_TruncatesMessagesBeyondConfiguredMaximum()
        {
            var target = new MockLogTarget();
            var logger = CreateLogger(target, CreateUtilitySupplier(0), string.Empty, 5);

            logger.LogInfo("123456789");

            Assert.That(target.LoggedEntries.Count, Is.EqualTo(1));
            Assert.That(target.LoggedEntries[0].Message, Is.EqualTo("12345\n--Truncated--"));
        }

        [Test]
        public void LogFormat_WithInvalidGenericFormat_DoesNotThrowIntoCaller()
        {
            Assert.DoesNotThrow(() => _logger.LogFormat(LogLevel.Info, "invalid {0", 7));

            Assert.That(_logTarget.LoggedEntries.Count, Is.EqualTo(1));
            Assert.That(_logTarget.LoggedEntries[0].Message, Does.Contain("cannot be formatted"));
        }

        [Test]
        public void LogInfo_WhenTargetThrows_ContinuesToNextTargetAndMutesFailingTarget()
        {
            var failingTarget = new ThrowingLogTarget();
            var captureTarget = new MockLogTarget();
            var logger = new CoreLogger("Gameplay",
                                        string.Empty,
                                        new ILogTarget[] { failingTarget, captureTarget },
                                        CreateUtilitySupplier(0),
                                        256);

            Assert.DoesNotThrow(() =>
            {
                logger.LogInfo("first");
                logger.LogInfo("second");
                logger.LogInfo("third");
                logger.LogInfo("fourth");
            });

            Assert.That(failingTarget.LogCallCount, Is.EqualTo(3));
            Assert.That(failingTarget.IsLogLevelAllowed(LogLevel.Info, "Gameplay"), Is.False);
            Assert.That(captureTarget.LoggedEntries.Count, Is.EqualTo(4));
        }

        [Test]
        public void LogInfo_WhenTargetLogsRecursively_DropsRecursiveEntry()
        {
            var reentrantTarget = new ReentrantLogTarget();
            var logger = new CoreLogger("Gameplay",
                                        string.Empty,
                                        new ILogTarget[] { reentrantTarget },
                                        CreateUtilitySupplier(0),
                                        256);
            reentrantTarget.Logger = logger;

            Assert.DoesNotThrow(() => logger.LogInfo("outer"));

            Assert.That(reentrantTarget.LogCallCount, Is.EqualTo(1));
        }

        [Test]
        public void LogInfo_OffMainThread_WhenSingleDispatchSubFlagIsDisabled_SkipsUnsafeTarget()
        {
            var target = new MockLogTarget();
            target.ApplyConfiguration(new MockLogTargetConfiguration
            {
                MinLogLevel = LogLevel.Debug,
                IsThreadSafe = false,
                DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration
                {
                    Enabled = true,
                    SingleLogDispatchEnabled = false,
                    BatchLogsDispatchEnabled = false
                }
            });
            var logger = CreateLogger(target, CreateUtilitySupplier(0), string.Empty, 256);

            RunOnWorkerThread(() => logger.LogInfo("unsafe"));

            Assert.That(target.LoggedEntries, Is.Empty);
        }

        private static CoreLogger CreateLogger(MockLogTarget logTarget,
                                               UtilitySupplier utilitySupplier,
                                               string subCategoryName,
                                               uint messageMaxLength)
        {
            return new CoreLogger("Gameplay", subCategoryName, new ILogTarget[] { logTarget }, utilitySupplier, messageMaxLength);
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

        private sealed class ThrowingLogTarget : LogTarget
        {
            public int LogCallCount { get; private set; }
            public override string LogTargetConfigurationName => nameof(MockLogTargetConfiguration);

            public ThrowingLogTarget()
            {
                ApplyConfiguration(new MockLogTargetConfiguration
                {
                    MinLogLevel = LogLevel.Debug,
                    IsThreadSafe = true
                });
            }

            public override void Log(LogLevel level,
                                     string category,
                                     string message,
                                     LogAttributes logAttributes,
                                     Exception exception)
            {
                LogCallCount++;
                throw new InvalidOperationException("Target failure");
            }

            public override void LogBatch(IReadOnlyList<LogEntry> logBatch)
            {
                throw new InvalidOperationException("Target failure");
            }
        }

        private sealed class ReentrantLogTarget : LogTarget
        {
            public ILogger Logger { get; set; }
            public int LogCallCount { get; private set; }
            public override string LogTargetConfigurationName => nameof(MockLogTargetConfiguration);

            public ReentrantLogTarget()
            {
                ApplyConfiguration(new MockLogTargetConfiguration
                {
                    MinLogLevel = LogLevel.Debug,
                    IsThreadSafe = true
                });
            }

            public override void Log(LogLevel level,
                                     string category,
                                     string message,
                                     LogAttributes logAttributes,
                                     Exception exception)
            {
                LogCallCount++;
                Logger.LogInfo("recursive");
            }

            public override void LogBatch(IReadOnlyList<LogEntry> logBatch)
            {
            }
        }
    }
}
