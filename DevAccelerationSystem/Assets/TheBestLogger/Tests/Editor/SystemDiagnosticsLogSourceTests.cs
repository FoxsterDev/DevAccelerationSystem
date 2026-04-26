using System;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;

namespace TheBestLogger.Tests.Editor
{
    [TestFixture]
    public sealed class SystemDiagnosticsLogSourceTests
    {
        private RecordingLogConsumer _consumer;
        private TextWriter _originalOut;
        private TextWriter _originalError;

        [SetUp]
        public void SetUp()
        {
            _consumer = new RecordingLogConsumer();
            _originalOut = Console.Out;
            _originalError = Console.Error;
        }

        [TearDown]
        public void TearDown()
        {
            Console.SetOut(_originalOut);
            Console.SetError(_originalError);
        }

        [Test]
        public void SystemDiagnosticsDebugLogSource_WriteAndWriteLine_AreCaptured()
        {
            using var source = new SystemDiagnosticsDebugLogSource(_consumer);

            source.Write("alpha");
            source.WriteLine("beta");

            Assert.That(_consumer.Count, Is.EqualTo(2));
            Assert.That(_consumer.Calls[0].LogSourceId, Is.EqualTo(nameof(SystemDiagnosticsDebugLogSource)));
            Assert.That(_consumer.Calls[0].Message, Is.EqualTo("alpha"));
            Assert.That(_consumer.Calls[1].Message, Is.EqualTo("beta"));
        }

        [Test]
        public void SystemDiagnosticsConsoleLogSource_RedirectsOutAndErrorAndRestoresOnDispose()
        {
            using var initialOut = new StringWriter();
            using var initialError = new StringWriter();
            Console.SetOut(initialOut);
            Console.SetError(initialError);
            var expectedOut = Console.Out;
            var expectedError = Console.Error;

            var source = new SystemDiagnosticsConsoleLogSource(_consumer);

            Console.Write("alpha");
            Console.Error.WriteLine("beta");

            Assert.That(_consumer.Count, Is.EqualTo(2));
            Assert.That(_consumer.Calls[0].LogLevel, Is.EqualTo(LogLevel.Debug));
            Assert.That(_consumer.Calls[0].LogSourceId, Is.EqualTo(nameof(SystemDiagnosticsConsoleLogSource)));
            Assert.That(_consumer.Calls[0].Message, Is.EqualTo("alpha"));
            Assert.That(_consumer.Calls[1].LogLevel, Is.EqualTo(LogLevel.Error));
            Assert.That(_consumer.Calls[1].Message, Is.EqualTo("beta"));

            source.Dispose();

            Assert.That(Console.Out, Is.SameAs(expectedOut));
            Assert.That(Console.Error, Is.SameAs(expectedError));
            Assert.That(initialOut.ToString(), Is.EqualTo("alpha"));
            Assert.That(initialError.ToString(), Is.EqualTo("beta" + Environment.NewLine));
        }

        [Test]
        public void SystemDiagnosticsConsoleRedirector_WriteLine_LogsAndForwardsToOriginalWriter()
        {
            using var sink = new StringWriter();
            Console.SetOut(sink);

            using var redirector = new SystemDiagnosticsConsoleRedirector(_consumer, false);
            redirector.WriteLine("hello");

            Assert.That(_consumer.Count, Is.EqualTo(1));
            Assert.That(_consumer.LastCall.LogLevel, Is.EqualTo(LogLevel.Debug));
            Assert.That(_consumer.LastCall.Message, Is.EqualTo("hello"));
            Assert.That(sink.ToString(), Is.EqualTo("hello" + Environment.NewLine));
        }
    }
}
