using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TheBestLogger.Tests.PlayMode
{
    [TestFixture]
    public sealed class PlatformTargetRuntimePlayModeTests
    {
        private GameObject _contextObject;

        [SetUp]
        public void SetUp()
        {
            _contextObject = new GameObject("PlatformTargetContext");
            AppleSystemLogTarget.ResetTestHooks();
            AndroidSystemLogTarget.ResetTestHooks();
        }

        [TearDown]
        public void TearDown()
        {
            AppleSystemLogTarget.ResetTestHooks();
            AndroidSystemLogTarget.ResetTestHooks();

            if (_contextObject != null)
            {
                Object.DestroyImmediate(_contextObject);
            }
        }

        [UnityTest]
        public IEnumerator UnityEditorConsoleLogTarget_RuntimePath_UsesUnityLogHandler()
        {
            var originalHandler = Debug.unityLogger.logHandler;
            var capture = new PlayModeUnityLogHandlerCapture();
            Debug.unityLogger.logHandler = capture;

            try
            {
                var target = new UnityEditorConsoleLogTarget();
                var attributes = new LogAttributes(LogImportance.Important)
                {
                    UnityContextObject = _contextObject,
                    Tags = new[] { "playmode" }
                };

                target.Log(LogLevel.Warning, "Gameplay", "runtime-warning", attributes);
                target.Log(LogLevel.Exception, "Gameplay", "runtime-exception", new LogAttributes(_contextObject), new InvalidOperationException("boom"));

                yield return null;

                Assert.That(capture.FormatCalls.Count, Is.EqualTo(1));
                Assert.That(capture.FormatCalls[0].LogType, Is.EqualTo(LogType.Warning));
                Assert.That(capture.FormatCalls[0].Context, Is.EqualTo(_contextObject));
                Assert.That(capture.FormatCalls[0].Args[0] as string, Does.Contain("[Gameplay] runtime-warning"));
                Assert.That(capture.ExceptionCalls.Count, Is.EqualTo(1));
                Assert.That(capture.ExceptionCalls[0].Exception, Is.TypeOf<InvalidOperationException>());
            }
            finally
            {
                Debug.unityLogger.logHandler = originalHandler;
            }
        }

        [UnityTest]
        public IEnumerator AppleSystemLogTarget_RuntimePath_UsesBridgeForLevelsExceptionsAndBatch()
        {
            var bridge = new CapturingAppleBridge();
            AppleSystemLogTarget.Bridge = bridge;

            var target = new AppleSystemLogTarget("com.foxster.app", "Unity");
            var exception = new InvalidOperationException("boom");
            var debugAttributes = new LogAttributes(LogImportance.Important);
            debugAttributes.Tags = new[] { "playmode", "apple" };
            debugAttributes.Add("attempt", 5);
            var exceptionAttributes = new LogAttributes(LogImportance.Critical) { StackTrace = "captured-stack" };
            exceptionAttributes.Add("traceId", "ios-42");

            target.Log(LogLevel.Debug, "Gameplay", "debug-message", debugAttributes);
            target.Log(LogLevel.Exception, "Gameplay", "ignored", exceptionAttributes, exception);
            target.LogBatch(new List<LogEntry>
            {
                new(LogLevel.Info, "Bootstrap", "batch-one", new LogAttributes(), null),
                new(LogLevel.Warning, "Runtime", "batch-two", new LogAttributes(), null)
            });

            yield return null;

            Assert.That(bridge.Initializations.Count, Is.EqualTo(1));
            Assert.That(bridge.Initializations[0], Is.EqualTo(("com.foxster.app", "Unity")));
            Assert.That(bridge.Calls.Count, Is.EqualTo(4));
            Assert.That(bridge.Calls[0].Method, Is.EqualTo(AppleSystemLogMethod.Debug));
            Assert.That(bridge.Calls[0].Category, Is.EqualTo("Gameplay"));
            Assert.That(bridge.Calls[0].Message, Does.StartWith("debug-message"));
            Assert.That(bridge.Calls[0].Message, Does.Contain("Importance: Important"));
            Assert.That(bridge.Calls[0].Message, Does.Contain("Tags: playmode, apple"));
            Assert.That(bridge.Calls[0].Message, Does.Contain("attempt: 5"));
            Assert.That(bridge.Calls[1].Method, Is.EqualTo(AppleSystemLogMethod.Error));
            Assert.That(bridge.Calls[1].Message, Does.StartWith("ignored"));
            Assert.That(bridge.Calls[1].Message, Does.Contain("Importance: Critical"));
            Assert.That(bridge.Calls[1].Message, Does.Contain("traceId: ios-42"));
            Assert.That(bridge.Calls[1].Message, Does.Contain("--- Exception ---"));
            Assert.That(bridge.Calls[1].Message, Does.Contain("boom\ncaptured-stack"));
            Assert.That(bridge.Calls[2], Is.EqualTo((AppleSystemLogMethod.Info, "Bootstrap", "batch-one")));
            Assert.That(bridge.Calls[3], Is.EqualTo((AppleSystemLogMethod.Default, "Runtime", "batch-two")));
        }

        [UnityTest]
        public IEnumerator AndroidSystemLogTarget_RuntimePath_UsesBridgeForPayloadAndBatch()
        {
            var bridge = new CapturingAndroidBridge();
            AndroidSystemLogTarget.Bridge = bridge;

            var target = new AndroidSystemLogTarget("Foxster");
            var attributes = new LogAttributes(LogImportance.Critical);
            attributes.Tags = new[] { "core" };
            attributes.Add("attempt", 2);

            target.Log(LogLevel.Warning, "Gameplay", "warn-message", attributes);
            target.LogBatch(new List<LogEntry>
            {
                new(LogLevel.Info, "Bootstrap", "batch-one", new LogAttributes(), null),
                new(LogLevel.Exception, "Runtime", "batch-two", new LogAttributes(), new InvalidOperationException("boom"))
            });

            yield return null;

            Assert.That(bridge.InitializedTags, Is.EqualTo(new[] { "Foxster" }));
            Assert.That(bridge.Calls.Count, Is.EqualTo(3));
            Assert.That(bridge.Calls[0].Method, Is.EqualTo(AndroidSystemLogMethod.Warning));
            Assert.That(bridge.Calls[0].Message, Does.StartWith("[Gameplay] warn-message"));
            Assert.That(bridge.Calls[0].Message, Does.Contain("Importance: Critical"));
            Assert.That(bridge.Calls[0].Message, Does.Contain("Tags: core"));
            Assert.That(bridge.Calls[0].Message, Does.Contain("Props: - attempt: 2"));
            Assert.That(bridge.Calls[1].Method, Is.EqualTo(AndroidSystemLogMethod.Info));
            Assert.That(bridge.Calls[1].Message, Does.StartWith("[Bootstrap] batch-one"));
            Assert.That(bridge.Calls[2].Method, Is.EqualTo(AndroidSystemLogMethod.Error));
            Assert.That(bridge.Calls[2].Message, Does.Contain("--- Exception ---"));
        }

        private sealed class CapturingAppleBridge : IAppleSystemLogBridge
        {
            public List<(string Subsystem, string Category)> Initializations { get; } = new();
            public List<(AppleSystemLogMethod Method, string Category, string Message)> Calls { get; } = new();

            public void Initialize(string subsystem, string category)
            {
                Initializations.Add((subsystem, category));
            }

            public void LogDefault(string category, string message)
            {
                Calls.Add((AppleSystemLogMethod.Default, category, message));
            }

            public void LogInfo(string category, string message)
            {
                Calls.Add((AppleSystemLogMethod.Info, category, message));
            }

            public void LogDebug(string category, string message)
            {
                Calls.Add((AppleSystemLogMethod.Debug, category, message));
            }

            public void LogError(string category, string message)
            {
                Calls.Add((AppleSystemLogMethod.Error, category, message));
            }
        }

        private sealed class CapturingAndroidBridge : IAndroidSystemLogBridge
        {
            public List<string> InitializedTags { get; } = new();
            public List<(AndroidSystemLogMethod Method, string Message)> Calls { get; } = new();

            public void Initialize(string globalTag)
            {
                InitializedTags.Add(globalTag);
            }

            public void Log(AndroidSystemLogMethod method, string message)
            {
                Calls.Add((method, message));
            }
        }

        private sealed class PlayModeUnityLogHandlerCapture : ILogHandler
        {
            public List<(LogType LogType, Object Context, string Format, object[] Args)> FormatCalls { get; } = new();
            public List<(Exception Exception, Object Context)> ExceptionCalls { get; } = new();

            public void LogFormat(LogType logType, Object context, string format, params object[] args)
            {
                FormatCalls.Add((logType, context, format, args));
            }

            public void LogException(Exception exception, Object context)
            {
                ExceptionCalls.Add((exception, context));
            }
        }
    }
}
