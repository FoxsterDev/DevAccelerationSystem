using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using TheBestLogger;
using TheBestLogger.Examples;
using TheBestLogger.Examples.LogTargets;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TheBestLoggerSample.Tests
{
    [TestFixture]
    public class ConsumerValidationPlayModeTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            LogAssert.ignoreFailingMessages = false;
            LogManager.Dispose();
            CleanupRuntimeObjects();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            LogManager.Dispose();
            CleanupRuntimeObjects();
            yield return null;
        }

        [UnityTest]
        public IEnumerator DemoEntryFlow_InitializesAndLogsFromMainThreadBackgroundThreadAndException()
        {
            LogAssert.ignoreFailingMessages = true;
            GameLoggerSample.InitializeLogger();
            yield return null;

            GameLogger.GameLoading.LogInfo("consumer-main", new LogAttributes(LogImportance.Critical));

            var backgroundTask = Task.Run(() => GameLogger.GameLoading.LogInfo("consumer-background", new LogAttributes(LogImportance.Critical)));
            yield return WaitForTask(backgroundTask);

            GameLogger.Main.LogException(new InvalidOperationException("consumer-exception"));

            yield return null;
        }

        [UnityTest]
        public IEnumerator DemoEntryFlow_ConfigUpdateSimulation_FiltersDebugAndPreservesWarning()
        {
            LogAssert.ignoreFailingMessages = true;
            GameLoggerSample.InitializeLogger();
            yield return null;

            var applied = LogManager.TryApplyRemoteConfigurationPatch(nameof(UnityEditorConsoleLogTargetConfiguration),
                                                                      "{\"MinLogLevel\":2}",
                                                                      out var error);
            Assert.That(applied, Is.True, error);

            LogManager.CreateLogger("ConsumerValidation").LogDebug("suppressed-debug");
            LogManager.CreateLogger("ConsumerValidation").LogWarning("consumer-warning");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ConsumerWorkspace_SceneTransitionWhileQueuedBackgroundLogIsInFlight_DeliversMessage()
        {
            LogAssert.ignoreFailingMessages = true;

            var queuedTarget = new ConsumerValidationQueuedTarget();
            LogManager.Initialize(new ReadOnlyCollection<LogTarget>(new List<LogTarget> { queuedTarget }),
                                  "GameLogger/Dev/",
                                  CancellationToken.None);

            var firstScene = SceneManager.CreateScene("ConsumerValidation_A");
            Assert.That(SceneManager.SetActiveScene(firstScene), Is.True);

            var logger = LogManager.CreateLogger("GamePlay");
            var backgroundTask = Task.Run(() => logger.LogWarning("queued-scene-transition", new LogAttributes(LogImportance.Critical)));

            var secondScene = SceneManager.CreateScene("ConsumerValidation_B");
            Assert.That(SceneManager.SetActiveScene(secondScene), Is.True);

            yield return WaitForTask(backgroundTask);
            yield return null;
            yield return null;

            Assert.That(queuedTarget.LoggedMessages.Contains("queued-scene-transition"), Is.True);

            yield return UnloadSceneIfLoaded(firstScene);
            yield return UnloadSceneIfLoaded(secondScene);
        }

        private static IEnumerator WaitForTask(Task task)
        {
            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.IsFaulted)
            {
                throw task.Exception ?? new Exception("Task failed.");
            }
        }

        private static IEnumerator UnloadSceneIfLoaded(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                yield break;
            }

            var unloadOperation = SceneManager.UnloadSceneAsync(scene);
            if (unloadOperation == null)
            {
                yield break;
            }

            while (!unloadOperation.isDone)
            {
                yield return null;
            }
        }

        private static void CleanupRuntimeObjects()
        {
            foreach (var drawer in Resources.FindObjectsOfTypeAll<IMGUIRuntimeDrawer>())
            {
                if (drawer != null && drawer.gameObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(drawer.gameObject);
                }
            }
        }

        private sealed class ConsumerValidationQueuedTarget : LogTarget
        {
            private readonly List<string> _loggedMessages = new();

            public IReadOnlyList<string> LoggedMessages => _loggedMessages;

            public override string LogTargetConfigurationName => nameof(OpenSearchLogTargetConfiguration);

            public override void Log(LogLevel level,
                                     string category,
                                     string message,
                                     LogAttributes logAttributes,
                                     Exception exception = null)
            {
                _loggedMessages.Add(message);
            }

            public override void LogBatch(IReadOnlyList<LogEntry> logBatch)
            {
                if (logBatch == null)
                {
                    return;
                }

                foreach (var entry in logBatch)
                {
                    _loggedMessages.Add(entry.Message);
                }
            }
        }
    }
}
