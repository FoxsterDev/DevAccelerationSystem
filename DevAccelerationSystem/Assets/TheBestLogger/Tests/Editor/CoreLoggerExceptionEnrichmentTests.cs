using System;
using System.Linq;
using NUnit.Framework;
using TheBestLogger.Core.Utilities;
using UnityEngine;

namespace TheBestLogger.Tests.Editor
{
    [TestFixture]
    public class CoreLoggerExceptionEnrichmentTests
    {
        private const string DefaultCategory = "Uncategorized";
        private const string UnhandledSourceId = nameof(UnobservedUniTaskExceptionLogSource);

        private MockLogTarget _logTarget;
        private UtilitySupplier _utilitySupplier;
        private CoreLogger _logger;
        private ILogConsumer _consumer;

        [SetUp]
        public void SetUp()
        {
            _logTarget = new MockLogTarget();
            _utilitySupplier = new UtilitySupplier(0,
                new StackTraceFormatter(Application.dataPath, new StackTraceFormatterConfiguration()));
            _logger = new CoreLogger(DefaultCategory, string.Empty, new ILogTarget[] { _logTarget }, _utilitySupplier, 2048);
            _consumer = _logger;
        }

        // A1 — message derived from the exception only when empty.

        [Test]
        public void UnhandledException_WithEmptyMessage_PopulatesMessageFromException()
        {
            _consumer.LogFormat(LogLevel.Exception, UnhandledSourceId, string.Empty,
                new ObjectDisposedException("CancellationTokenSource"));

            var entry = _logTarget.LoggedEntries.Single();
            Assert.That(entry.Message, Is.Not.Empty);
            Assert.That(entry.Message, Does.Contain(nameof(ObjectDisposedException)));
            Assert.That(entry.Message, Does.Contain("CancellationTokenSource"));
        }

        [Test]
        public void UnhandledException_WithNonEmptyMessage_DoesNotOverwriteMessage()
        {
            _consumer.LogFormat(LogLevel.Exception, UnhandledSourceId, "checkout failed", new InvalidOperationException("boom"));

            var entry = _logTarget.LoggedEntries.Single();
            Assert.That(entry.Message, Is.EqualTo("checkout failed"));
            Assert.That(entry.Message, Does.Not.Contain(nameof(InvalidOperationException)));
        }

        [Test]
        public void UnhandledException_TruncatesLongDerivedMessage()
        {
            var target = new MockLogTarget();
            ILogConsumer consumer = new CoreLogger(DefaultCategory, string.Empty, new ILogTarget[] { target }, _utilitySupplier, 8);

            consumer.LogFormat(LogLevel.Exception, UnhandledSourceId, string.Empty,
                new InvalidOperationException(new string('x', 500)));

            Assert.That(target.LoggedEntries.Single().Message, Does.EndWith("--Truncated--"));
        }

        // A2 — captured exceptions emit their source id as category; public and non-exception logs are untouched.

        [Test]
        public void UnhandledException_UsesLogSourceIdAsCategory_NotUncategorized()
        {
            _consumer.LogFormat(LogLevel.Exception, UnhandledSourceId, string.Empty, new InvalidOperationException("boom"));

            var entry = _logTarget.LoggedEntries.Single();
            Assert.That(entry.Category, Is.EqualTo(UnhandledSourceId));
            Assert.That(entry.Category, Is.Not.EqualTo(DefaultCategory));
        }

        [Test]
        public void PublicLogException_KeepsRealCategory_AndEnriches()
        {
            _logger.LogException(new InvalidOperationException("boom"));

            var entry = _logTarget.LoggedEntries.Single();
            Assert.That(entry.Category, Is.EqualTo(DefaultCategory));
            Assert.That(entry.Category, Is.Not.EqualTo("direct"));

            var props = entry.Attributes.Props;
            Assert.That(props.Single(p => p.Key == "ExceptionType").Value, Is.EqualTo(typeof(InvalidOperationException).FullName));
            Assert.That((string) props.Single(p => p.Key == "Fingerprint").Value, Is.Not.Empty);
        }

        [Test]
        public void NonExceptionLog_KeepsDefaultCategory_AndAddsNoExceptionAttributes()
        {
            _consumer.LogFormat(LogLevel.Warning, UnhandledSourceId, "plain-warning", null);

            var entry = _logTarget.LoggedEntries.Single();
            Assert.That(entry.Category, Is.EqualTo(DefaultCategory));
            Assert.That(entry.Message, Is.EqualTo("plain-warning"));
            Assert.That(entry.Attributes.Props == null ||
                        entry.Attributes.Props.All(p => p.Key != "ExceptionType" && p.Key != "Fingerprint"),
                Is.True);
        }

        // A3 — ExceptionType + Fingerprint attributes.

        [Test]
        public void UnhandledException_AddsExceptionTypeAndFingerprintAttributes()
        {
            _consumer.LogFormat(LogLevel.Exception, UnhandledSourceId, string.Empty, new InvalidOperationException("boom"));

            var props = _logTarget.LoggedEntries.Single().Attributes.Props;
            Assert.That(props, Is.Not.Null);
            Assert.That(props.Single(p => p.Key == "ExceptionType").Value, Is.EqualTo(typeof(InvalidOperationException).FullName));
            Assert.That((string) props.Single(p => p.Key == "Fingerprint").Value, Does.Match("^[0-9A-F]{8}$"));
        }

        [Test]
        public void AggregateException_ReportsInnerType_NotAggregate()
        {
            _consumer.LogFormat(LogLevel.Exception, nameof(UnobservedTaskExceptionLogSource), string.Empty,
                new AggregateException(new InvalidOperationException("boom")));

            var props = _logTarget.LoggedEntries.Single().Attributes.Props;
            Assert.That(props.Single(p => p.Key == "ExceptionType").Value, Is.EqualTo(typeof(InvalidOperationException).FullName));
        }

        [Test]
        public void MultiTarget_EnrichesExactlyOnce()
        {
            var t1 = new MockLogTarget();
            var t2 = new MockLogTarget();
            ILogConsumer consumer = new CoreLogger(DefaultCategory, string.Empty, new ILogTarget[] { t1, t2 }, _utilitySupplier, 2048);

            consumer.LogFormat(LogLevel.Exception, UnhandledSourceId, string.Empty, new InvalidOperationException("boom"));

            Assert.That(t1.LoggedEntries.Count, Is.EqualTo(1));
            Assert.That(t2.LoggedEntries.Count, Is.EqualTo(1));
            Assert.That(t1.LoggedEntries[0].Attributes.Props.Count(p => p.Key == "ExceptionType"), Is.EqualTo(1));
        }

        // Fingerprint contract.

        [Test]
        public void Fingerprint_IsStableAcrossOccurrencesFromSameSite()
        {
            Assert.That(ExceptionFingerprint.Compute(ThrowSiteA()), Is.EqualTo(ExceptionFingerprint.Compute(ThrowSiteA())));
            Assert.That(ExceptionFingerprint.Compute(ThrowSiteA()), Does.Match("^[0-9A-F]{8}$"));
        }

        [Test]
        public void Fingerprint_DiffersByExceptionType()
        {
            Assert.That(ExceptionFingerprint.Compute(new ObjectDisposedException("x")),
                Is.Not.EqualTo(ExceptionFingerprint.Compute(new NullReferenceException("x"))));
        }

        [Test]
        public void Fingerprint_IncorporatesAppStackFrame()
        {
            // A thrown exception carries an app frame; an unthrown one collapses to type-only -> the keys must differ.
            Assert.That(ExceptionFingerprint.Compute(ThrowSiteA()),
                Is.Not.EqualTo(ExceptionFingerprint.Compute(new InvalidOperationException("x"))));
        }

        [Test]
        public void Fingerprint_DiffersByCallSite()
        {
            Assert.That(ExceptionFingerprint.Compute(ThrowSiteA()), Is.Not.EqualTo(ExceptionFingerprint.Compute(ThrowSiteB())));
        }

        [Test]
        public void Fingerprint_UnwrapsAggregateExceptionToInner()
        {
            var wrapped = ExceptionFingerprint.Compute(new AggregateException(new InvalidOperationException("x")));
            var direct = ExceptionFingerprint.Compute(new InvalidOperationException("x"));
            var otherInner = ExceptionFingerprint.Compute(new AggregateException(new TimeoutException("x")));

            Assert.That(wrapped, Is.EqualTo(direct));
            Assert.That(wrapped, Is.Not.EqualTo(otherInner));
        }

        [Test]
        public void Fingerprint_NullException_ReturnsFallbackAndDoesNotThrow()
        {
            Assert.That(ExceptionFingerprint.Compute(null), Is.EqualTo("00000000"));
        }

        private static InvalidOperationException ThrowSiteA()
        {
            try { throw new InvalidOperationException("x"); }
            catch (InvalidOperationException e) { return e; }
        }

        private static InvalidOperationException ThrowSiteB()
        {
            try { throw new InvalidOperationException("x"); }
            catch (InvalidOperationException e) { return e; }
        }
    }
}
