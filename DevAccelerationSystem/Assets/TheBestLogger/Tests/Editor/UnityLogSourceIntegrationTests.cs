using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using Debug = UnityEngine.Debug;

namespace TheBestLogger.Tests.Editor
{
    [TestFixture]
    public sealed class UnityLogSourceIntegrationTests
    {
        private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;

        private RecordingLogConsumer _consumer;
        private ILogHandler _originalHandler;
        private LogType _originalFilterLogType;
        private bool _originalLogAssertIgnoreFailingMessages;
        private GameObject _contextObject;

        [SetUp]
        public void SetUp()
        {
            _consumer = new RecordingLogConsumer();
            _originalHandler = Debug.unityLogger.logHandler;
            _originalFilterLogType = Debug.unityLogger.filterLogType;
            _originalLogAssertIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;
            _contextObject = new GameObject("UnityLogSourceContext");
        }

        [TearDown]
        public void TearDown()
        {
            Debug.unityLogger.logHandler = _originalHandler;
            Debug.unityLogger.filterLogType = _originalFilterLogType;
            LogAssert.ignoreFailingMessages = _originalLogAssertIgnoreFailingMessages;

            if (_contextObject != null)
            {
                Object.DestroyImmediate(_contextObject);
            }
        }

        [Test]
        public void UnityDebugLogSource_OverridesAndRestoresUnityLogHandler()
        {
            using var source = new UnityDebugLogSource(_consumer);

            Assert.That(Debug.unityLogger.logHandler, Is.SameAs(source));

            source.Dispose();

            Assert.That(Debug.unityLogger.logHandler, Is.SameAs(_originalHandler));
            Assert.That(Debug.unityLogger.filterLogType, Is.EqualTo(_originalFilterLogType));
        }

        [Test]
        public void UnityDebugLogSource_CapturesFormattedMessagesAndExceptionsWithoutDuplicates()
        {
            using var source = new UnityDebugLogSource(_consumer);

            Debug.unityLogger.LogFormat(LogType.Warning, _contextObject, "{0}-{1}", "alpha", 42);
            Debug.unityLogger.LogException(new InvalidOperationException("boom"), _contextObject);

            Assert.That(_consumer.Count, Is.EqualTo(2));

            var formatCall = _consumer.Calls[0];
            Assert.That(formatCall.LogLevel, Is.EqualTo(LogLevel.Warning));
            Assert.That(formatCall.LogSourceId, Is.EqualTo(nameof(UnityDebugLogSource)));
            Assert.That(formatCall.Message, Is.EqualTo("{0}-{1}"));
            Assert.That(formatCall.Context, Is.EqualTo(_contextObject));
            Assert.That(formatCall.Args, Is.EqualTo(new object[] { "alpha", 42 }));

            var exceptionCall = _consumer.Calls[1];
            Assert.That(exceptionCall.LogLevel, Is.EqualTo(LogLevel.Exception));
            Assert.That(exceptionCall.Exception, Is.TypeOf<InvalidOperationException>());
            Assert.That(exceptionCall.Context, Is.EqualTo(_contextObject));
        }

        [Test]
        public void UnityApplicationLogSource_InvokesMappedLogLevelAndStackTrace()
        {
            using var source = new UnityApplicationLogSource(_consumer);

            InvokePrivate(source,
                          "OnLogMessageReceived",
                          "hello",
                          "stack line",
                          LogType.Error);

            Assert.That(_consumer.Count, Is.EqualTo(1));
            Assert.That(_consumer.LastCall.LogLevel, Is.EqualTo(LogLevel.Error));
            Assert.That(_consumer.LastCall.LogSourceId, Is.EqualTo(nameof(UnityApplicationLogSource)));
            Assert.That(_consumer.LastCall.Message, Is.EqualTo("hello"));
            Assert.That(_consumer.LastCall.StackTrace, Is.EqualTo("stack line"));
        }

        [Test]
        public void UnityApplicationLogSource_DisposePreventsFurtherUnityLogCapture()
        {
            var source = new UnityApplicationLogSource(_consumer);

            Debug.Log("before-dispose");
            Assert.That(_consumer.Count, Is.EqualTo(1));

            source.Dispose();
            Debug.Log("after-dispose");

            Assert.That(_consumer.Count, Is.EqualTo(1));
        }

        [Test]
        public void UnityApplicationLogSourceThreaded_InvokesMappedLogLevelAndStackTrace()
        {
            using var source = new UnityApplicationLogSourceThreaded(_consumer);

            InvokePrivate(source,
                          "OnLogMessageReceivedThreaded",
                          "threaded",
                          "thread-stack",
                          LogType.Exception);

            Assert.That(_consumer.Count, Is.EqualTo(1));
            Assert.That(_consumer.LastCall.LogLevel, Is.EqualTo(LogLevel.Exception));
            Assert.That(_consumer.LastCall.LogSourceId, Is.EqualTo(nameof(UnityApplicationLogSourceThreaded)));
            Assert.That(_consumer.LastCall.Message, Is.EqualTo("threaded"));
            Assert.That(_consumer.LastCall.StackTrace, Is.EqualTo("thread-stack"));
        }

        [Test]
        public void UnityApplicationLogSourceThreaded_DisposePreventsFurtherUnityLogCapture()
        {
            var source = new UnityApplicationLogSourceThreaded(_consumer);

            Debug.Log("before-threaded-dispose");
            Assert.That(_consumer.Count, Is.EqualTo(1));

            source.Dispose();
            Debug.Log("after-threaded-dispose");

            Assert.That(_consumer.Count, Is.EqualTo(1));
        }

        private static void InvokePrivate(object target, string methodName, params object[] args)
        {
            target.GetType()
                  .GetMethod(methodName, InstancePrivate)
                  ?.Invoke(target, args);
        }
    }
}
