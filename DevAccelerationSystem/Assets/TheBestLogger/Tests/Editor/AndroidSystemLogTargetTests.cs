using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace TheBestLogger.Tests.Editor
{
    [TestFixture]
    public sealed class AndroidSystemLogTargetTests
    {
        [TestCase(LogLevel.Debug, (int)AndroidSystemLogMethod.Debug)]
        [TestCase(LogLevel.Info, (int)AndroidSystemLogMethod.Info)]
        [TestCase(LogLevel.Warning, (int)AndroidSystemLogMethod.Warning)]
        [TestCase(LogLevel.Error, (int)AndroidSystemLogMethod.Error)]
        [TestCase(LogLevel.Exception, (int)AndroidSystemLogMethod.Error)]
        public void MapLogLevel_ReturnsExpectedNativeMethod(LogLevel level, int expectedMethod)
        {
            Assert.That((int)AndroidSystemLogTarget.MapLogLevel(level), Is.EqualTo(expectedMethod));
        }

        [Test]
        public void BuildMessagePayload_FormatsCategoryMessageAndAttributes()
        {
            var attributes = new LogAttributes(LogImportance.Critical);
            attributes.Tags = new[] { "core", "android" };
            attributes.Add("attempt", 3);

            var result = AndroidSystemLogTarget.BuildMessagePayload("Gameplay", "hello", attributes, null);

            Assert.That(result, Does.StartWith("[Gameplay] hello"));
            Assert.That(result, Does.Contain("Tags: core, android"));
            Assert.That(result, Does.Contain("Props: - attempt: 3"));
        }

        [Test]
        public void BuildMessagePayload_AppendsExceptionText()
        {
            var exception = new InvalidOperationException("boom");

            var result = AndroidSystemLogTarget.BuildMessagePayload("Gameplay", "hello", null, exception);

            Assert.That(result, Does.Contain("[Gameplay] hello"));
            Assert.That(result, Does.Contain("--- Exception ---"));
            Assert.That(result, Does.Contain("InvalidOperationException: boom"));
        }

        [Test]
        public void BuildMessagePayload_WhenMessageIsNull_ReturnsEmptyString()
        {
            var result = AndroidSystemLogTarget.BuildMessagePayload(null, null, null, null);

            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [Test]
        public void LogBatch_WhenNull_DoesNotThrow()
        {
            var target = new SpyAndroidSystemLogTarget();

            Assert.DoesNotThrow(() => target.LogBatch(null));
            Assert.That(target.LogCalls, Is.EqualTo(0));
        }

        [Test]
        public void LogBatch_ForwardsEachEntryToLog()
        {
            var target = new SpyAndroidSystemLogTarget();
            var entries = new List<LogEntry>
            {
                new(LogLevel.Info, "Bootstrap", "first", new LogAttributes(), null),
                new(LogLevel.Exception, "Gameplay", "second", new LogAttributes(), new InvalidOperationException("boom"))
            };

            target.LogBatch(entries);

            Assert.That(target.LogCalls, Is.EqualTo(2));
            Assert.That(target.ForwardedEntries, Has.Count.EqualTo(2));
            Assert.That(target.ForwardedEntries[0].Category, Is.EqualTo("Bootstrap"));
            Assert.That(target.ForwardedEntries[1].Exception, Is.TypeOf<InvalidOperationException>());
        }

        private sealed class SpyAndroidSystemLogTarget : AndroidSystemLogTarget
        {
            public SpyAndroidSystemLogTarget() : base("Unity")
            {
            }

            public int LogCalls { get; private set; }
            public List<LogEntry> ForwardedEntries { get; } = new();

            public override void Log(LogLevel level, string category, string message, LogAttributes logAttributes = null, Exception exception = null)
            {
                LogCalls++;
                ForwardedEntries.Add(new LogEntry(level, category, message, logAttributes, exception));
            }
        }
    }
}
