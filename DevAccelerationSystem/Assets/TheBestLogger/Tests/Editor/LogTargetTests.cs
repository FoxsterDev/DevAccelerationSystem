using System;
using NUnit.Framework;
using TheBestLogger.Tests.Editor;

namespace TheBestLogger.Core.Tests
{
    [TestFixture]
    public class LogTargetTests
    {
        private ILogTarget _logTarget;

        [SetUp]
        public void Setup()
        {
            _logTarget = new MockLogTarget();
        }

        [Test]
        public void Mute_ShouldMuteLogTarget()
        {
            // Arrange
            _logTarget.Mute(true);

            // Act
            var result = _logTarget.IsLogLevelAllowed(LogLevel.Info, "TestCategory");

            // Assert
            Assert.IsFalse(result, "LogTarget should not allow log levels when muted.");
        }

        [Test]
        public void IsStackTraceEnabled_ShouldReturnFalse_WhenConfigurationIsNull()
        {
            // Act
            var result = _logTarget.IsStackTraceEnabled(LogLevel.Info, "TestCategory");

            // Assert
            Assert.IsFalse(result, "Stack trace should not be enabled if configuration is null.");
        }

        [Test]
        public void IsLogLevelAllowed_ShouldReturnTrue_WhenLogLevelIsAboveMinLevel()
        {
            // Arrange
            var config = new MockLogTargetConfiguration
            {
                MinLogLevel = LogLevel.Info,
                Muted = false
            };
            _logTarget.ApplyConfiguration(config);

            // Act
            var result = _logTarget.IsLogLevelAllowed(LogLevel.Warning, "TestCategory");

            // Assert
            Assert.IsTrue(result, "LogTarget should allow log levels greater than or equal to min log level.");
        }

        [Test]
        public void IsLogLevelAllowed_ShouldReturnFalse_WhenMuted()
        {
            // Arrange
            var config = new MockLogTargetConfiguration
            {
                MinLogLevel = LogLevel.Info,
                Muted = true
            };
            _logTarget.ApplyConfiguration(config);

            // Act
            var result = _logTarget.IsLogLevelAllowed(LogLevel.Warning, "TestCategory");

            // Assert
            Assert.IsFalse(result, "LogTarget should not allow any log levels when muted.");
        }

        [Test]
        public void ApplyConfiguration_ShouldUpdateConfigurationCorrectly()
        {
            // Arrange
            var config = new MockLogTargetConfiguration
            {
                MinLogLevel = LogLevel.Warning,
                Muted = true
            };

            // Act
            _logTarget.ApplyConfiguration(config);

            // Assert
            Assert.AreEqual(LogLevel.Warning, _logTarget.Configuration.MinLogLevel);
            Assert.IsTrue(_logTarget.Configuration.Muted);
        }

        [Test]
        public void SetDebugMode_ShouldEnableDebugMode()
        {
            // Act
            _logTarget.SetDebugMode(true);

            // Assert
            Assert.IsTrue(((MockLogTarget) _logTarget).DebugModeEnabled, "Debug mode should be enabled.");
        }
    }
}
