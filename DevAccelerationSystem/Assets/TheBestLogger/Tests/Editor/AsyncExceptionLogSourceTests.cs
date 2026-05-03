using System;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;

namespace TheBestLogger.Tests.Editor
{
    [TestFixture]
    public sealed class AsyncExceptionLogSourceTests
    {
        private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;

        private RecordingLogConsumer _consumer;

        [SetUp]
        public void SetUp()
        {
            _consumer = new RecordingLogConsumer();
        }

        [Test]
        public void UnobservedTaskExceptionLogSource_LogsAggregateExceptionAndMarksObserved()
        {
            using var source = new UnobservedTaskExceptionLogSource(_consumer);
            var exception = new InvalidOperationException("boom");
            var aggregateException = new AggregateException(exception);
            var args = new UnobservedTaskExceptionEventArgs(aggregateException);

            InvokePrivate(source, "OnUnobservedTaskException", null, args);

            Assert.That(args.Observed, Is.True);
            Assert.That(_consumer.Count, Is.EqualTo(1));
            Assert.That(_consumer.LastCall.LogLevel, Is.EqualTo(LogLevel.Exception));
            Assert.That(_consumer.LastCall.LogSourceId, Is.EqualTo(nameof(UnobservedTaskExceptionLogSource)));
            Assert.That(_consumer.LastCall.Exception, Is.TypeOf<AggregateException>());
            Assert.That(_consumer.LastCall.Exception?.InnerException, Is.SameAs(exception));
        }

        [Test]
        public void UnobservedUniTaskExceptionLogSource_LogsExceptionAndNullFallback()
        {
            using var source = new UnobservedUniTaskExceptionLogSource(_consumer, new UniTaskConfiguration());

            InvokePrivate(source, "OnUnobservedTaskException", new InvalidOperationException("boom"));
            Assert.That(_consumer.Count, Is.EqualTo(1));
            Assert.That(_consumer.LastCall.Exception, Is.TypeOf<InvalidOperationException>());

            InvokePrivate(source, "OnUnobservedTaskException", new object[] { null });
            Assert.That(_consumer.Count, Is.EqualTo(2));
            Assert.That(_consumer.LastCall.Message, Is.EqualTo("UnobservedUniTaskException is null"));
            Assert.That(_consumer.LastCall.Exception, Is.Null);
        }

        [Test]
        public void CurrentDomainUnhandledExceptionLogSource_LogsException()
        {
            using var source = new CurrentDomainUnhandledExceptionLogSource(_consumer);
            var exception = new InvalidOperationException("boom");
            var args = new UnhandledExceptionEventArgs(exception, true);

            InvokePrivate(source, "OnUnhandledException", this, args);

            Assert.That(_consumer.Count, Is.EqualTo(1));
            Assert.That(_consumer.LastCall.LogLevel, Is.EqualTo(LogLevel.Exception));
            Assert.That(_consumer.LastCall.LogSourceId, Is.EqualTo(nameof(CurrentDomainUnhandledExceptionLogSource)));
            Assert.That(_consumer.LastCall.Exception, Is.SameAs(exception));
        }

        [Test]
        public void CurrentDomainUnhandledExceptionLogSource_WhenExceptionObjectIsNotException_LogsFallbackError()
        {
            using var source = new CurrentDomainUnhandledExceptionLogSource(_consumer);
            var args = new UnhandledExceptionEventArgs("fatal-string", true);

            InvokePrivate(source, "OnUnhandledException", this, args);

            Assert.That(_consumer.Count, Is.EqualTo(1));
            Assert.That(_consumer.LastCall.LogLevel, Is.EqualTo(LogLevel.Exception));
            Assert.That(_consumer.LastCall.Message, Is.EqualTo("fatal-string"));
            Assert.That(_consumer.LastCall.Exception, Is.Null);
        }

        private static void InvokePrivate(object target, string methodName, params object[] args)
        {
            target.GetType()
                  .GetMethod(methodName, InstancePrivate)
                  ?.Invoke(target, args);
        }
    }
}
