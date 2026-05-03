using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace TheBestLogger.Tests.Editor
{
    [TestFixture]
    public class AppleSystemLogTargetTests
    {
        [Test]
        public void BuildExceptionMessage_UsesLogAttributesStackTraceWhenAvailable()
        {
            var exception = new InvalidOperationException("boom");
            var logAttributes = new LogAttributes { StackTrace = "captured stack" };

            var result = AppleSystemLogTarget.BuildExceptionMessage(exception, logAttributes);

            Assert.That(result, Is.EqualTo("boom\ncaptured stack"));
        }

        [Test]
        public void BuildExceptionMessage_FallsBackToExceptionStackTraceWhenAttributesAreMissing()
        {
            var exception = new InvalidOperationException("boom");
            exception = CaptureException(exception);

            var result = AppleSystemLogTarget.BuildExceptionMessage(exception, null);

            Assert.That(result, Does.StartWith("boom\n"));
            Assert.That(result, Does.Contain(nameof(CaptureException)));
        }

        [TestCase(LogLevel.Debug, (int)AppleSystemLogMethod.Debug)]
        [TestCase(LogLevel.Info, (int)AppleSystemLogMethod.Info)]
        [TestCase(LogLevel.Warning, (int)AppleSystemLogMethod.Default)]
        [TestCase(LogLevel.Error, (int)AppleSystemLogMethod.Error)]
        [TestCase(LogLevel.Exception, (int)AppleSystemLogMethod.Error)]
        public void MapLogLevel_ReturnsExpectedNativeMethod(LogLevel level, int expectedMethod)
        {
            Assert.That((int)AppleSystemLogTarget.MapLogLevel(level), Is.EqualTo(expectedMethod));
        }

        [Test]
        public void BuildExceptionMessage_ReturnsMessageWhenNoStackTraceExists()
        {
            var exception = new Exception("boom");

            var result = AppleSystemLogTarget.BuildExceptionMessage(exception, new LogAttributes());

            Assert.That(result, Is.EqualTo("boom"));
        }

        [Test]
        public void BuildMessagePayload_AppendsFormattedAttributes()
        {
            var attributes = new LogAttributes(LogImportance.Critical);
            attributes.Tags = new[] { "ios", "native" };
            attributes.Add("attempt", 7);

            var result = AppleSystemLogTarget.BuildMessagePayload("hello", attributes);

            Assert.That(result, Does.StartWith("hello"));
            Assert.That(result, Does.Contain("Importance: Critical"));
            Assert.That(result, Does.Contain("Tags: ios, native"));
            Assert.That(result, Does.Contain("attempt: 7"));
        }

        [Test]
        public void BuildExceptionPayload_AppendsMessageAttributesAndExceptionText()
        {
            var attributes = new LogAttributes(LogImportance.Important);
            attributes.Add("session", "abc");
            var exception = new InvalidOperationException("boom");
            exception = CaptureException(exception);

            var messagePayload = AppleSystemLogTarget.BuildMessagePayload("hello", attributes);
            var result = AppleSystemLogTarget.BuildExceptionPayload(messagePayload, exception, attributes);

            Assert.That(result, Does.StartWith("hello"));
            Assert.That(result, Does.Contain("Importance: Important"));
            Assert.That(result, Does.Contain("session: abc"));
            Assert.That(result, Does.Contain("--- Exception ---"));
            Assert.That(result, Does.Contain("boom\n"));
            Assert.That(result, Does.Contain(nameof(CaptureException)));
        }

        [Test]
        public void LogBatch_WhenNull_DoesNotThrow()
        {
            var target = new SpyAppleSystemLogTarget();

            Assert.DoesNotThrow(() => target.LogBatch(null));
            Assert.That(target.LogCalls, Is.EqualTo(0));
        }

        [Test]
        public void LogBatch_ForwardsEachEntryToLog()
        {
            var target = new SpyAppleSystemLogTarget();
            var entries = new List<LogEntry>
            {
                new(LogLevel.Info, "Bootstrap", "first", new LogAttributes(), null),
                new(LogLevel.Error, "Gameplay", "second", new LogAttributes(), new InvalidOperationException("boom"))
            };

            target.LogBatch(entries);

            Assert.That(target.LogCalls, Is.EqualTo(2));
            Assert.That(target.ForwardedEntries, Has.Count.EqualTo(2));
            Assert.That(target.ForwardedEntries[0].Category, Is.EqualTo("Bootstrap"));
            Assert.That(target.ForwardedEntries[1].Exception, Is.TypeOf<InvalidOperationException>());
        }

        private static InvalidOperationException CaptureException(InvalidOperationException exception)
        {
            try
            {
                throw exception;
            }
            catch (InvalidOperationException ex)
            {
                return ex;
            }
        }

        private sealed class SpyAppleSystemLogTarget : AppleSystemLogTarget
        {
            public SpyAppleSystemLogTarget() : base("subsystem", "category")
            {
            }

            public int LogCalls { get; private set; }
            public List<LogEntry> ForwardedEntries { get; } = new();

            public override void Log(LogLevel level, string category, string message, LogAttributes logAttributes, Exception exception = null)
            {
                LogCalls++;
                ForwardedEntries.Add(new LogEntry(level, category, message, logAttributes, exception));
            }
        }
    }
}
