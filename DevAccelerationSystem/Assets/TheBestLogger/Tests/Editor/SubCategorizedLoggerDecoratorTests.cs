using System;
using NUnit.Framework;
using TheBestLogger.Core.Utilities;
using UnityEngine;

namespace TheBestLogger.Tests.Editor
{
    [TestFixture]
    public sealed class SubCategorizedLoggerDecoratorTests
    {
        [Test]
        public void LogError_WithException_PreservesExceptionObject()
        {
            var logTarget = new MockLogTarget();
            var logger = CreateLogger(logTarget);
            var decorator = new SubCategorizedLoggerDecorator("[Net]", logger);
            var exception = new InvalidOperationException("boom");

            Assert.DoesNotThrow(() => decorator.LogError("request failed", exception));

            Assert.That(logTarget.LoggedEntries.Count, Is.EqualTo(1));
            Assert.That(logTarget.LoggedEntries[0].Exception, Is.SameAs(exception));
            Assert.That(logTarget.LoggedEntries[0].Message, Does.Contain("[Net]"));
        }

        [Test]
        public void LogFormat_GenericOverloadWithoutAttributes_DoesNotThrowAndFormatsMessage()
        {
            var logTarget = new MockLogTarget();
            var logger = CreateLogger(logTarget);
            var decorator = new SubCategorizedLoggerDecorator("[Net]", logger);

            Assert.DoesNotThrow(() => decorator.LogFormat(LogLevel.Info, "value {0}", 42));

            Assert.That(logTarget.LoggedEntries.Count, Is.EqualTo(1));
            Assert.That(logTarget.LoggedEntries[0].Message, Does.Contain("[Net]"));
            Assert.That(logTarget.LoggedEntries[0].Message, Does.Contain("42"));
        }

        private static CoreLogger CreateLogger(MockLogTarget logTarget)
        {
            return new CoreLogger("Gameplay",
                                  string.Empty,
                                  new ILogTarget[] { logTarget },
                                  new UtilitySupplier(0, new StackTraceFormatter(Application.dataPath, new StackTraceFormatterConfiguration())),
                                  512);
        }
    }
}
