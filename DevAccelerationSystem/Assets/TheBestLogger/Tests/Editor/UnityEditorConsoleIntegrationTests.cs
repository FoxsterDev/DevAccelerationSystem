using System;
using System.Collections.Generic;
using NUnit.Framework;
using Object = UnityEngine.Object;
using UnityEngine;

namespace TheBestLogger.Tests.Editor
{
    [TestFixture]
    public sealed class UnityEditorConsoleIntegrationTests
    {
        private ILogHandler _originalLogHandler;
        private CapturingUnityLogHandler _capturingLogHandler;
        private UnityEditorConsoleLogTarget _target;
        private GameObject _contextObject;

        [SetUp]
        public void SetUp()
        {
            _originalLogHandler = Debug.unityLogger.logHandler;
            _capturingLogHandler = new CapturingUnityLogHandler();
            Debug.unityLogger.logHandler = _capturingLogHandler;
            _target = new UnityEditorConsoleLogTarget();
            _contextObject = new GameObject("LoggerContext");
        }

        [TearDown]
        public void TearDown()
        {
            _target?.Dispose();
            Debug.unityLogger.logHandler = _originalLogHandler;

            if (_contextObject != null)
            {
                Object.DestroyImmediate(_contextObject);
            }
        }

        [TestCase(LogLevel.Info, LogType.Log)]
        [TestCase(LogLevel.Debug, LogType.Log)]
        [TestCase(LogLevel.Warning, LogType.Warning)]
        [TestCase(LogLevel.Error, LogType.Error)]
        public void Log_MapsLevelToUnityLogTypeAndPreservesContext(LogLevel level, LogType expectedLogType)
        {
            var attributes = new LogAttributes(LogImportance.Critical)
            {
                UnityContextObject = _contextObject,
                Tags = new[] { "consumer" }
            };

            _target.Log(level, "Gameplay", "hello", attributes);

            Assert.That(_capturingLogHandler.FormatCalls.Count, Is.EqualTo(1));
            Assert.That(_capturingLogHandler.ExceptionCalls.Count, Is.EqualTo(0));

            var call = _capturingLogHandler.FormatCalls[0];
            Assert.That(call.LogType, Is.EqualTo(expectedLogType));
            Assert.That(call.Context, Is.EqualTo(_contextObject));
            Assert.That(call.Format, Is.EqualTo("{0}"));
            Assert.That(call.Args, Has.Length.EqualTo(1));

            var message = call.Args[0] as string;
            Assert.That(message, Does.Contain("[Gameplay] hello"));
            Assert.That(message, Does.Contain("Importance: Critical"));
            Assert.That(message, Does.Contain("Tags: consumer"));
            Assert.That(message, Does.Contain("Context: LoggerContext (GameObject)"));
        }

        [Test]
        public void Log_Exception_ForwardsExceptionAndContext()
        {
            var exception = new InvalidOperationException("boom");
            var attributes = new LogAttributes(_contextObject);

            _target.Log(LogLevel.Exception, "Gameplay", "ignored", attributes, exception);

            Assert.That(_capturingLogHandler.FormatCalls, Is.Empty);
            Assert.That(_capturingLogHandler.ExceptionCalls.Count, Is.EqualTo(1));
            Assert.That(_capturingLogHandler.ExceptionCalls[0].Exception, Is.SameAs(exception));
            Assert.That(_capturingLogHandler.ExceptionCalls[0].Context, Is.EqualTo(_contextObject));
        }

        [Test]
        public void LogBatch_SingleEntry_UsesSingleLogPath()
        {
            var attributes = new LogAttributes(LogImportance.Critical)
            {
                UnityContextObject = _contextObject
            };

            _target.LogBatch(new List<LogEntry>
            {
                new(LogLevel.Warning, "Bootstrap", "single-entry", attributes, null)
            });

            Assert.That(_capturingLogHandler.FormatCalls.Count, Is.EqualTo(1));
            Assert.That(_capturingLogHandler.FormatCalls[0].LogType, Is.EqualTo(LogType.Warning));
            Assert.That(_capturingLogHandler.FormatCalls[0].Context, Is.EqualTo(_contextObject));
            Assert.That(_capturingLogHandler.FormatCalls[0].Args[0] as string, Does.Contain("[Bootstrap] single-entry"));
        }

        [Test]
        public void LogBatch_MultipleEntries_EmitsCombinedBatchMessage()
        {
            _target.LogBatch(new List<LogEntry>
            {
                new(LogLevel.Info, "Bootstrap", "first", new LogAttributes(), null),
                new(LogLevel.Error, "Gameplay", "second", new LogAttributes(), null)
            });

            Assert.That(_capturingLogHandler.FormatCalls.Count, Is.EqualTo(1));
            Assert.That(_capturingLogHandler.ExceptionCalls, Is.Empty);

            var call = _capturingLogHandler.FormatCalls[0];
            Assert.That(call.LogType, Is.EqualTo(LogType.Log));
            Assert.That(call.Context, Is.Null);
            Assert.That(call.Args[0] as string, Is.EqualTo("batch[:0] [Info] [Bootstrap] first\nbatch[:1] [Error] [Gameplay] second\n"));
        }

        [Test]
        public void Constructor_CachesCurrentUnityLogHandler()
        {
            var firstHandler = new CapturingUnityLogHandler();
            Debug.unityLogger.logHandler = firstHandler;

            using var target = new UnityEditorConsoleLogTarget();

            var secondHandler = new CapturingUnityLogHandler();
            Debug.unityLogger.logHandler = secondHandler;

            target.Log(LogLevel.Info, "Gameplay", "cached-handler", new LogAttributes());

            Assert.That(firstHandler.FormatCalls.Count, Is.EqualTo(1));
            Assert.That(secondHandler.FormatCalls, Is.Empty);
        }

        private sealed class CapturingUnityLogHandler : ILogHandler
        {
            public List<FormatCall> FormatCalls { get; } = new();
            public List<ExceptionCall> ExceptionCalls { get; } = new();

            public void LogFormat(LogType logType, Object context, string format, params object[] args)
            {
                FormatCalls.Add(new FormatCall(logType, context, format, args));
            }

            public void LogException(Exception exception, Object context)
            {
                ExceptionCalls.Add(new ExceptionCall(exception, context));
            }
        }

        private sealed class FormatCall
        {
            public FormatCall(LogType logType, Object context, string format, object[] args)
            {
                LogType = logType;
                Context = context;
                Format = format;
                Args = args;
            }

            public LogType LogType { get; }
            public Object Context { get; }
            public string Format { get; }
            public object[] Args { get; }
        }

        private sealed class ExceptionCall
        {
            public ExceptionCall(Exception exception, Object context)
            {
                Exception = exception;
                Context = context;
            }

            public Exception Exception { get; }
            public Object Context { get; }
        }
    }
}
