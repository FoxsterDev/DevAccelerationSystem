using System;
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
    }
}
