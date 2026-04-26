using System;
using System.Linq;
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
    }
}
