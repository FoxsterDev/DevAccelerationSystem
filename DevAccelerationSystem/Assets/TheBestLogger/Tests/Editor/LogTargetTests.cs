using System;
using System.Collections.Generic;
using NUnit.Framework;
using TheBestLogger;

namespace TheBestLogger.Tests.Editor
{
    [TestFixture]
    public class LogTargetTests
    {
        private ILogTarget _logTarget;
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
            _logTarget.DebugModeEnabled = true;

            // Act
            var result = _logTarget.IsLogLevelAllowed(LogLevel.Debug, "TestDebugCategory");

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsLogLevelAllowed_WhenDebugModeEnabledAndLogLevelHigherDebugMinLevel_ReturnsTrue()
        {
            // Arrange
            _logTarget.DebugModeEnabled = true;

            // Act
            var result = _logTarget.IsLogLevelAllowed(LogLevel.Info, "TestCategory");

            // Assert
            Assert.IsTrue(result);
        }
        
        [Test]
        public void IsLogLevelAllowed_WhenDebugModeEnabledAndLogLevelBelowDebugMinLevel_ReturnsFalse()
        {
            // Arrange
            _logTarget.DebugModeEnabled = true;

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
        public void IsLogLevelAllowed_WhenOverrideCategorySessionRolloutIsZero_OverrideStillApplies()
        {
            _logTarget.ApplyConfiguration(new MockLogTargetConfiguration
            {
                MinLogLevel = LogLevel.Error,
                OverrideCategories = new[]
                {
                    new LogTargetCategory
                    {
                        Category = "SampledCategory",
                        MinLevel = LogLevel.Debug,
                        SessionRolloutPercentage = 0f
                    }
                }
            });

            var result = _logTarget.IsLogLevelAllowed(LogLevel.Debug, "SampledCategory");

            Assert.IsTrue(result);
        }

        [Test]
        public void IsLogLevelAllowed_WhenOverrideCategorySessionRolloutBlocksCurrentSession_ReturnsFalse()
        {
            var categoryName = FindCategoryNameForDecision((MockLogTarget) _logTarget, expectedAllowed: false);
            var rolloutPercentage = CreateRolloutPercentageForDecision((MockLogTarget) _logTarget, categoryName, expectedAllowed: false);

            _logTarget.ApplyConfiguration(new MockLogTargetConfiguration
            {
                MinLogLevel = LogLevel.Error,
                OverrideCategories = new[]
                {
                    new LogTargetCategory
                    {
                        Category = categoryName,
                        MinLevel = LogLevel.Debug,
                        SessionRolloutPercentage = rolloutPercentage
                    }
                }
            });

            var result = _logTarget.IsLogLevelAllowed(LogLevel.Debug, categoryName);

            Assert.IsFalse(result);
            Assert.IsFalse(_logTarget.IsLogLevelAllowed(LogLevel.Debug, "AnotherCategory"));
        }

        [Test]
        public void IsLogLevelAllowed_WhenDebugOverrideCategorySessionRolloutBlocksCurrentSession_ReturnsFalse()
        {
            var categoryName = FindDebugCategoryNameForDecision((MockLogTarget) _logTarget, expectedAllowed: false);
            var rolloutPercentage = CreateDebugRolloutPercentageForDecision((MockLogTarget) _logTarget, categoryName, expectedAllowed: false);

            _logTarget.DebugModeEnabled = true;
            _logTarget.ApplyConfiguration(new MockLogTargetConfiguration
            {
                MinLogLevel = LogLevel.Error,
                DebugMode = new DebugModeConfiguration
                {
                    Enabled = true,
                    MinLogLevel = LogLevel.Info,
                    OverrideCategories = new[]
                    {
                        new LogTargetCategory
                        {
                            Category = categoryName,
                            MinLevel = LogLevel.Debug,
                            SessionRolloutPercentage = rolloutPercentage
                        }
                    }
                }
            });

            var result = _logTarget.IsLogLevelAllowed(LogLevel.Debug, categoryName);

            Assert.IsFalse(result);
        }

        [Test]
        public void ApplyConfiguration_WhenOverrideCategorySessionRolloutIsReapplied_ReRollsCurrentSessionDecision()
        {
            var target = (MockLogTarget) _logTarget;
            var categoryName = FindCategoryNameForReapplyDecisionChange(target);
            var firstRolloutPercentage = CreateRolloutPercentageForDecision(target, categoryName, expectedAllowed: false);

            _logTarget.ApplyConfiguration(new MockLogTargetConfiguration
            {
                MinLogLevel = LogLevel.Error,
                OverrideCategories = new[]
                {
                    new LogTargetCategory
                    {
                        Category = categoryName,
                        MinLevel = LogLevel.Debug,
                        SessionRolloutPercentage = firstRolloutPercentage
                    }
                }
            });

            Assert.IsFalse(_logTarget.IsLogLevelAllowed(LogLevel.Debug, categoryName));

            var secondRolloutPercentage = CreateRolloutPercentageForDecision(target, categoryName, expectedAllowed: true);
            _logTarget.ApplyConfiguration(new MockLogTargetConfiguration
            {
                MinLogLevel = LogLevel.Error,
                OverrideCategories = new[]
                {
                    new LogTargetCategory
                    {
                        Category = categoryName,
                        MinLevel = LogLevel.Debug,
                        SessionRolloutPercentage = secondRolloutPercentage
                    }
                }
            });

            Assert.IsTrue(_logTarget.IsLogLevelAllowed(LogLevel.Debug, categoryName));
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
            _logTarget.DebugModeEnabled = true;

            // Assert
            Assert.IsTrue(_logTarget.DebugModeEnabled);
        }

        private static string FindCategoryNameForDecision(MockLogTarget target, bool expectedAllowed)
        {
            for (var index = 1; index < 10000; index++)
            {
                var categoryName = $"SampledCategory_{index}";
                var bucket = RolloutSampler.ComputeBucketPercentage(target.CategoryRolloutSessionKey,
                                                                    target.NextConfigurationApplyVersion,
                                                                    0,
                                                                    categoryName);
                if (expectedAllowed)
                {
                    if (bucket < 99f)
                    {
                        return categoryName;
                    }
                }
                else if (bucket > 1f)
                {
                    return categoryName;
                }
            }

            Assert.Fail($"Could not find category for expected decision {expectedAllowed}.");
            return string.Empty;
        }

        private static string FindDebugCategoryNameForDecision(MockLogTarget target, bool expectedAllowed)
        {
            for (var index = 1; index < 10000; index++)
            {
                var categoryName = $"SampledDebugCategory_{index}";
                var bucket = RolloutSampler.ComputeBucketPercentage(target.CategoryRolloutSessionKey,
                                                                    target.NextConfigurationApplyVersion,
                                                                    0,
                                                                    categoryName);
                if (expectedAllowed)
                {
                    if (bucket < 99f)
                    {
                        return categoryName;
                    }
                }
                else if (bucket > 1f)
                {
                    return categoryName;
                }
            }

            Assert.Fail($"Could not find debug category for expected decision {expectedAllowed}.");
            return string.Empty;
        }

        private static string FindCategoryNameForReapplyDecisionChange(MockLogTarget target)
        {
            for (var index = 1; index < 10000; index++)
            {
                var categoryName = $"ReapplyCategory_{index}";
                var firstBucket = RolloutSampler.ComputeBucketPercentage(target.CategoryRolloutSessionKey,
                                                                         target.NextConfigurationApplyVersion,
                                                                         0,
                                                                         categoryName);
                var secondBucket = RolloutSampler.ComputeBucketPercentage(target.CategoryRolloutSessionKey,
                                                                          target.NextConfigurationApplyVersion + 1,
                                                                          0,
                                                                          categoryName);
                if (firstBucket > 1f && secondBucket < 99f && Math.Abs(firstBucket - secondBucket) > 1f)
                {
                    return categoryName;
                }
            }

            Assert.Fail("Could not find category for reapply decision change.");
            return string.Empty;
        }

        private static float CreateRolloutPercentageForDecision(MockLogTarget target,
                                                                string categoryName,
                                                                bool expectedAllowed)
        {
            var bucket = RolloutSampler.ComputeBucketPercentage(target.CategoryRolloutSessionKey,
                                                                target.NextConfigurationApplyVersion,
                                                                0,
                                                                categoryName);
            return expectedAllowed
                ? Math.Min(99.99f, bucket + 0.5f)
                : Math.Max(0.01f, bucket - 0.5f);
        }

        private static float CreateDebugRolloutPercentageForDecision(MockLogTarget target,
                                                                     string categoryName,
                                                                     bool expectedAllowed)
        {
            var bucket = RolloutSampler.ComputeBucketPercentage(target.CategoryRolloutSessionKey,
                                                                target.NextConfigurationApplyVersion,
                                                                0,
                                                                categoryName);
            return expectedAllowed
                ? Math.Min(99.99f, bucket + 0.5f)
                : Math.Max(0.01f, bucket - 0.5f);
        }
    }
}
