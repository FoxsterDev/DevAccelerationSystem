using System;
using System.Collections.Generic;
using NUnit.Framework;
using TheBestLogger.Tests.Editor;

namespace TheBestLogger.Tests.Editor
{
    [TestFixture]
    public class LogTargetBatchLogsDecorationTests
    {
        private ILogTarget _logTargetDecoration;
        private MockLogTarget _mockLogTarget;
        private LogTargetBatchLogsConfiguration _config;

        [SetUp]
        public void SetUp()
        {
            _config = new LogTargetBatchLogsConfiguration { Enabled = true, MaxCountLogs = 10, UpdatePeriodMs = 1000 };
            _mockLogTarget = new MockLogTarget();
            _logTargetDecoration = new LogTargetBatchLogsDecoration(_config, _mockLogTarget, DateTime.UtcNow);
        }

        [Test]
        public void Log_CriticalImportance_LogImmediately()
        {
            // Arrange
            var logAttributes = new LogAttributes(LogImportance.Critical) { TimeUtc = DateTime.UtcNow };

            // Act
            ((ILogTarget) _logTargetDecoration).Log(LogLevel.Error, "TestCategory", "Critical message", logAttributes, null);

            // Assert
            Assert.AreEqual(1, _mockLogTarget.LoggedBatches.Count);
            Assert.AreEqual("Critical message", _mockLogTarget.LoggedBatches[0][0].Message);
        }

        [Test]
        public void Log_NiceToHaveImportance_StoresInNiceToHaveBag()
        {
            // Arrange
            var logAttributes = new LogAttributes(LogImportance.NiceToHave) { TimeUtc = DateTime.UtcNow };

            // Act
            ((ILogTarget) _logTargetDecoration).Log(LogLevel.Info, "TestCategory", "Nice to have message", logAttributes, null);

            // Assert
            Assert.AreEqual(0, _mockLogTarget.LoggedBatches.Count);
        }

        [Test]
        public void Update_WhenTimeExceeded_LogsBatch()
        {
            // Arrange
            var logAttributes = new LogAttributes(LogImportance.Important) { TimeUtc = DateTime.UtcNow };
            ((ILogTarget) _logTargetDecoration).Log(LogLevel.Info, "TestCategory", "Important message", logAttributes, null);

            // Act
            ((IScheduledUpdate) _logTargetDecoration).Update(DateTime.UtcNow.AddMilliseconds(_config.UpdatePeriodMs + 1), _config.UpdatePeriodMs + 1);

            // Assert
            Assert.AreEqual(1, _mockLogTarget.LoggedBatches.Count);
            Assert.AreEqual("Important message", _mockLogTarget.LoggedBatches[0][0].Message);
        }

        [Test]
        public void Update_WhenDifferentImportanceLevelsExist_PrioritizesImportantLogsBeforeNiceToHave()
        {
            var startTimeUtc = DateTime.UtcNow;

            ((ILogTarget) _logTargetDecoration).Log(LogLevel.Info,
                                                    "TestCategory",
                                                    "Nice to have message",
                                                    new LogAttributes(LogImportance.NiceToHave) { TimeUtc = startTimeUtc },
                                                    null);
            ((ILogTarget) _logTargetDecoration).Log(LogLevel.Info,
                                                    "TestCategory",
                                                    "Important message",
                                                    new LogAttributes(LogImportance.Important) { TimeUtc = startTimeUtc.AddMilliseconds(1) },
                                                    null);

            ((IScheduledUpdate) _logTargetDecoration).Update(startTimeUtc.AddMilliseconds(_config.UpdatePeriodMs + 1),
                                                             _config.UpdatePeriodMs + 1);

            Assert.AreEqual(1, _mockLogTarget.LoggedBatches.Count);
            Assert.AreEqual("Important message", _mockLogTarget.LoggedBatches[0][0].Message);
            Assert.AreEqual("Nice to have message", _mockLogTarget.LoggedBatches[0][1].Message);
        }

        [Test]
        public void ApplyRuntimeDefaults_WhenBatchValuesAreZero_ClampsToSafeValues()
        {
            var configuration = new MockLogTargetConfiguration
            {
                BatchLogs = new LogTargetBatchLogsConfiguration
                {
                    Enabled = true,
                    UpdatePeriodMs = 0,
                    MaxCountLogs = 0
                }
            };

            configuration.ApplyRuntimeDefaults();

            Assert.That(configuration.BatchLogs.UpdatePeriodMs, Is.EqualTo(100));
            Assert.That(configuration.BatchLogs.MaxCountLogs, Is.EqualTo(1));
        }

        [Test]
        public void BatchOverflow_MaxCountOne_MakesRealLogProgress()
        {
            var startTimeUtc = DateTime.UtcNow;
            var configuration = new LogTargetBatchLogsConfiguration
            {
                Enabled = true,
                MaxCountLogs = 1,
                UpdatePeriodMs = 100000
            };
            var target = new MockLogTarget();
            var decoration = new LogTargetBatchLogsDecoration(configuration, target, startTimeUtc);

            for (var index = 0; index < 300; index++)
            {
                ((ILogTarget) decoration).Log(LogLevel.Info,
                                              "TestCategory",
                                              "Nice to have message " + index,
                                              new LogAttributes(LogImportance.NiceToHave) { TimeUtc = startTimeUtc },
                                              null);
            }

            ((IScheduledUpdate) decoration).Update(startTimeUtc.AddMilliseconds(configuration.UpdatePeriodMs + 1),
                                                   configuration.UpdatePeriodMs + 1);

            Assert.That(target.LoggedEntries.Count, Is.EqualTo(2));
            Assert.That(target.LoggedEntries[0].Category, Is.EqualTo("TheBestLogger"));
            Assert.That(target.LoggedEntries[0].Message, Does.Contain("Dropped 44 buffered logs"));
            Assert.That(target.LoggedEntries[1].Category, Is.EqualTo("TestCategory"));
            Assert.That(target.LoggedEntries[1].Message, Does.StartWith("Nice to have message "));
        }
    }
}
