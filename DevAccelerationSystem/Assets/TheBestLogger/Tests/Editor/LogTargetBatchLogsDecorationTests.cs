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
            Assert.AreEqual("Critical message", _mockLogTarget.LoggedBatches[0][0].message);
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
            Assert.AreEqual("Important message", _mockLogTarget.LoggedBatches[0][0].message);
        }
    }
}
