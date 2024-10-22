using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;

namespace TheBestLogger.Tests.Editor
{
    [TestFixture]
    public class LogTargetDispatchingLogsToMainThreadDecorationTests
    {
        private ILogTarget _logDecoratedTarget;
        private MockLogTarget _mockLogTarget;
        private LogTargetDispatchingLogsToMainThreadConfiguration _config;
        private SynchronizationContext _synchronizationContext;
        private MockUtilitySupplier _utilitySupplier;

        [SetUp]
        public void SetUp()
        {
            _config = new LogTargetDispatchingLogsToMainThreadConfiguration
                { Enabled = true, SingleLogDispatchEnabled = true, BatchLogsDispatchEnabled = true };
            _mockLogTarget = new MockLogTarget();
            _synchronizationContext = new MakeSynchronizationContext();
            _utilitySupplier = new MockUtilitySupplier { IsMainThread = false };
            _logDecoratedTarget = new LogTargetDispatchingLogsToMainThreadDecoration(_config, _mockLogTarget, _synchronizationContext, _utilitySupplier);
        }

        [Test]
        public void Log_DispatchesToMainThread_WhenNotOnMainThread()
        {
            // Arrange
            _utilitySupplier.IsMainThread = false;
            var logAttributes = new LogAttributes(LogImportance.Critical) { TimeUtc = DateTime.UtcNow };

            // Act
            _logDecoratedTarget.Log(LogLevel.Error, "TestCategory", "Message to be dispatched", logAttributes, null);

            // Assert
            Assert.AreEqual(1, _mockLogTarget.LoggedBatches.Count);
            Assert.AreEqual("Message to be dispatched", _mockLogTarget.LoggedBatches[0][0].message);
        }

        [Test]
        public void Log_DoesNotDispatch_WhenOnMainThread()
        {
            // Arrange
            _utilitySupplier.IsMainThread = true;
            var logAttributes = new LogAttributes(LogImportance.Critical) { TimeUtc = DateTime.UtcNow };

            // Act
            _logDecoratedTarget.Log(LogLevel.Error, "TestCategory", "Message without dispatch", logAttributes, null);

            // Assert
            Assert.AreEqual(1, _mockLogTarget.LoggedBatches.Count);
            Assert.AreEqual("Message without dispatch", _mockLogTarget.LoggedBatches[0][0].message);
        }

        [Test]
        public void LogBatch_DispatchesToMainThread_WhenNotOnMainThread()
        {
            // Arrange
            _utilitySupplier.IsMainThread = false;
            var logBatch = new List<(LogLevel level, string category, string message, LogAttributes logAttributes, Exception exception)>
            {
                (LogLevel.Info, "TestCategory", "Batch message 1", new LogAttributes(LogImportance.Important), null),
                (LogLevel.Info, "TestCategory", "Batch message 2", new LogAttributes(LogImportance.Important), null)
            };

            // Act
            _logDecoratedTarget.LogBatch(logBatch);

            // Assert
            Assert.AreEqual(1, _mockLogTarget.LoggedBatches.Count);
            Assert.AreEqual(2, _mockLogTarget.LoggedBatches[0].Count);
            Assert.AreEqual("Batch message 1", _mockLogTarget.LoggedBatches[0][0].message);
            Assert.AreEqual("Batch message 2", _mockLogTarget.LoggedBatches[0][1].message);
        }

        [Test]
        public void LogBatch_DoesNotDispatch_WhenOnMainThread()
        {
            // Arrange
            _utilitySupplier.IsMainThread = true;
            var logBatch = new List<(LogLevel level, string category, string message, LogAttributes logAttributes, Exception exception)>
            {
                (LogLevel.Info, "TestCategory", "Batch message on main thread", new LogAttributes(LogImportance.Important), null)
            };

            // Act
            _logDecoratedTarget.LogBatch(logBatch);

            // Assert
            Assert.AreEqual(1, _mockLogTarget.LoggedBatches.Count);
            Assert.AreEqual("Batch message on main thread", _mockLogTarget.LoggedBatches[0][0].message);
        }

        [Test]
        public void ApplyConfiguration_UpdatesConfiguration()
        {
            // Arrange
            var newConfig = new MockLogTargetConfiguration
                { DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration { Enabled = false } };

            // Act
            _logDecoratedTarget.ApplyConfiguration(newConfig);

            // Assert
            Assert.IsFalse(_logDecoratedTarget.Configuration.DispatchingLogsToMainThread.Enabled);
        }
    }
}
