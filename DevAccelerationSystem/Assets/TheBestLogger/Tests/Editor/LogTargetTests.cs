using System;
using System.Collections.Generic;
using NUnit.Framework;
using TheBestLogger;

namespace TheBestLogger.Tests.Editor
{
    [TestFixture]
    public class LogTargetTests
    {
        private MockLogTarget _logTarget;
        private LogTargetConfiguration _config;

        [SetUp]
        public void SetUp()
        {
            _config = new MockLogTargetConfiguration
            {
                MinLogLevel = LogLevel.Warning,
                Muted = false,
                StackTraces = new LogLevelStackTraceConfiguration[]
                {
                    new LogLevelStackTraceConfiguration{ Level = LogLevel.Debug, Enabled = false},
                    new LogLevelStackTraceConfiguration{ Level = LogLevel.Info, Enabled = true},
                    new LogLevelStackTraceConfiguration{ Level = LogLevel.Warning, Enabled = true},
                    new LogLevelStackTraceConfiguration{ Level = LogLevel.Error, Enabled = true},
                    new LogLevelStackTraceConfiguration{ Level = LogLevel.Exception, Enabled = true}
                },
                OverrideCategories = new LogTargetCategory[1]{ new LogTargetCategory{Category = "TestOverrideCategory", MinLevel = LogLevel.Warning}},
                DebugMode = new DebugModeConfiguration
                {
                    Enabled = true,
                    MinLogLevel = LogLevel.Info,
                    OverrideCategories = new LogTargetCategory[1]{ new LogTargetCategory{Category = "TestDebugCategory", MinLevel = LogLevel.Debug}}
                }
            };

            _logTarget = new MockLogTarget();
            _logTarget.ApplyConfiguration(_config);
        }

        [Test]
        public void Mute_WhenCalled_SetsMutedState()
        {
            // Act
            _logTarget.Mute(true);

            // Assert
            Assert.IsFalse(_logTarget.IsLogLevelAllowed(LogLevel.Info, "TestCategory"));
        }

        [Test]
        public void IsStackTraceEnabled_WhenStackTraceEnabled_ReturnsTrue()
        {
            // Arrange
            var level = LogLevel.Error;

            // Act
            var result = _logTarget.IsStackTraceEnabled(level, "TestCategory");

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsStackTraceEnabled_WhenStackTraceDisabled_ReturnsFalse()
        {
            // Arrange
            var level = LogLevel.Debug;

            // Act
            var result = _logTarget.IsStackTraceEnabled(level, "TestCategory");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsLogLevelAllowed_WhenMuted_ReturnsFalse()
        {
            // Arrange
            _logTarget.Mute(true);

            // Act
            var result = _logTarget.IsLogLevelAllowed(LogLevel.Info, "TestCategory");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsLogLevelAllowed_WhenDebugModeEnabledAndLogLevelAllowed_ReturnsTrue()
        {
            // Arrange
            _logTarget.SetDebugMode(true);

            // Act
            var result = _logTarget.IsLogLevelAllowed(LogLevel.Debug, "TestDebugCategory");

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsLogLevelAllowed_WhenDebugModeEnabledAndLogLevelHigherDebugMinLevel_ReturnsTrue()
        {
            // Arrange
            _logTarget.SetDebugMode(true);

            // Act
            var result = _logTarget.IsLogLevelAllowed(LogLevel.Info, "TestCategory");

            // Assert
            Assert.IsTrue(result);
        }
        
        [Test]
        public void IsLogLevelAllowed_WhenDebugModeEnabledAndLogLevelBelowDebugMinLevel_ReturnsFalse()
        {
            // Arrange
            _logTarget.SetDebugMode(true);

            // Act
            var result = _logTarget.IsLogLevelAllowed(LogLevel.Debug, "TestCategory");

            // Assert
            Assert.IsFalse(result);
        }
        
        [Test]
        public void IsLogLevelAllowed_WhenLogLevelBelowMin_ReturnsFalse()
        {
            // Arrange
            _logTarget.ApplyConfiguration(new MockLogTargetConfiguration { MinLogLevel = LogLevel.Warning });

            // Act
            var result = _logTarget.IsLogLevelAllowed(LogLevel.Info, "TestCategory");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsLogLevelAllowed_WhenOverrideCategoryLogLevelHigherMin_ReturnsTrue()
        {
            // Arrange
            //_logTarget.ApplyConfiguration(new MockLogTargetConfiguration { MinLogLevel = LogLevel.Error });

            // Act
            var result = _logTarget.IsLogLevelAllowed(LogLevel.Warning, "TestOverrideCategory");

            // Assert
            Assert.IsTrue(result);
        }
 
        [Test]
        public void IsLogLevelAllowed_WhenOverrideCategoryLogLevelBelowMin_ReturnsFalse()
        {
            // Arrange
            //_logTarget.ApplyConfiguration(new MockLogTargetConfiguration { MinLogLevel = LogLevel.Error });

            // Act
            var result = _logTarget.IsLogLevelAllowed(LogLevel.Info, "TestOverrideCategory");

            // Assert
            Assert.IsFalse(result);
        }
 
        [Test]
        public void ApplyConfiguration_UpdatesLogLevelAndMutedState()
        {
            // Arrange
            var newConfig = new MockLogTargetConfiguration { MinLogLevel = LogLevel.Warning, Muted = true };

            // Act
            _logTarget.ApplyConfiguration(newConfig);

            // Assert
            Assert.AreEqual(LogLevel.Warning, _logTarget.Configuration.MinLogLevel);
            Assert.IsTrue(_logTarget.Configuration.Muted);
        }

        [Test]
        public void SetDebugMode_WhenEnabled_SetsDebugModeState()
        {
            // Act
            _logTarget.SetDebugMode(true);

            // Assert
            Assert.IsTrue(_logTarget.DebugModeEnabled);
        }
    }
}
