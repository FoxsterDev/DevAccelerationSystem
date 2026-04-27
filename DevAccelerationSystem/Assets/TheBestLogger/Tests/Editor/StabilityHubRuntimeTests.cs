using NUnit.Framework;
using StabilityHub;
using StabilityHub.Monitoring;
using System;
using System.Collections.Generic;

namespace TheBestLogger.Tests.Editor
{
    [TestFixture]
    public sealed class StabilityHubRuntimeTests
    {
        private SpyLogger _logger;

        [SetUp]
        public void SetUp()
        {
            _logger = new SpyLogger();
            StabilityHubService.ResetTestHooks();
        }

        [TearDown]
        public void TearDown()
        {
            new StabilityHubService().Dispose();
            StabilityHubService.ResetTestHooks();
            _logger.Dispose();
        }

        [Test]
        public void Initialize_WhenMonitoringConfigIsMissing_LogsDisabledAndDoesNotThrow()
        {
            StabilityHubService.MonitoringConfigLoader = () => null;

            Assert.DoesNotThrow(() => StabilityHubService.Initialize(_logger));

            Assert.That(_logger.DebugMessages, Is.EqualTo(new[] { "CrashReporterModule is disabled" }));
        }

        [Test]
        public void Initialize_WhenCrashReporterDisabled_DoesNotCreateModule()
        {
            var factoryCalls = 0;
            StabilityHubService.MonitoringConfigLoader = CreateMonitoringConfigurationLoader(moduleEnabled: false, iosEnabled: true);
            StabilityHubService.CrashReporterModuleFactory = _ =>
            {
                factoryCalls++;
                return new SpyCrashReporterModule();
            };

            StabilityHubService.Initialize(_logger);

            Assert.That(factoryCalls, Is.EqualTo(0));
            Assert.That(_logger.DebugMessages, Is.EqualTo(new[] { "CrashReporterModule is disabled" }));
        }

        [Test]
        public void Initialize_WhenIosCrashReporterEnabled_CreatesModuleAndLogsEnabled()
        {
            var spyModule = new SpyCrashReporterModule();
            StabilityHubService.MonitoringConfigLoader = CreateMonitoringConfigurationLoader(moduleEnabled: true, iosEnabled: true);
            StabilityHubService.CrashReporterModuleFactory = enabled =>
            {
                Assert.That(enabled, Is.True);
                return spyModule;
            };

            StabilityHubService.Initialize(_logger);

            Assert.That(_logger.DebugMessages, Is.EqualTo(new[] { "CrashReporterModule is enabled" }));
            Assert.That(spyModule.DisposeCallCount, Is.EqualTo(0));
        }

        [Test]
        public void RetrieveAndLogPreviousSessionIssues_ForwardsLoggerToModule()
        {
            var spyModule = new SpyCrashReporterModule();
            StabilityHubService.MonitoringConfigLoader = CreateMonitoringConfigurationLoader(moduleEnabled: true, iosEnabled: true);
            StabilityHubService.CrashReporterModuleFactory = _ => spyModule;

            StabilityHubService.Initialize(_logger);
            StabilityHubService.RetrieveAndLogPreviousSessionIssues();

            Assert.That(spyModule.RetrieveCallCount, Is.EqualTo(1));
            Assert.That(spyModule.LastLogger, Is.SameAs(_logger));
        }

        [Test]
        public void Dispose_DisposesCreatedModule()
        {
            var spyModule = new SpyCrashReporterModule();
            StabilityHubService.MonitoringConfigLoader = CreateMonitoringConfigurationLoader(moduleEnabled: true, iosEnabled: true);
            StabilityHubService.CrashReporterModuleFactory = _ => spyModule;
            StabilityHubService.Initialize(_logger);

            new StabilityHubService().Dispose();

            Assert.That(spyModule.DisposeCallCount, Is.EqualTo(1));
        }

        [Test]
        public void MonitoringConfiguration_IsIOSCrashReporterModuleEnabled_RequiresBothFlags()
        {
            var config = UnityEngine.ScriptableObject.CreateInstance<MonitoringConfiguration>();
            try
            {
                config.CrashReporterModule = new CrashReporterModuleConfiguration
                {
                    Enabled = true,
                    IOS = new CrashReporteriOSConfiguration { Enabled = true }
                };

                Assert.That(config.IsIOSCrashReporterModuleEnabled, Is.True);

                config.CrashReporterModule = new CrashReporterModuleConfiguration
                {
                    Enabled = true,
                    IOS = new CrashReporteriOSConfiguration { Enabled = false }
                };

                Assert.That(config.IsIOSCrashReporterModuleEnabled, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        private static Func<MonitoringConfiguration> CreateMonitoringConfigurationLoader(bool moduleEnabled, bool iosEnabled)
        {
            return () =>
            {
                var config = UnityEngine.ScriptableObject.CreateInstance<MonitoringConfiguration>();
                config.CrashReporterModule = new CrashReporterModuleConfiguration
                {
                    Enabled = moduleEnabled,
                    IOS = new CrashReporteriOSConfiguration { Enabled = iosEnabled }
                };
                return config;
            };
        }

        private sealed class SpyCrashReporterModule : ICrashReporterModule
        {
            public int RetrieveCallCount { get; private set; }
            public int DisposeCallCount { get; private set; }
            public ILogger LastLogger { get; private set; }

            public void RetrieveAndLogPreviousSessionIssues(ILogger logger)
            {
                RetrieveCallCount++;
                LastLogger = logger;
            }

            public void Dispose()
            {
                DisposeCallCount++;
            }
        }

        private sealed class SpyLogger : ILogger
        {
            public List<string> DebugMessages { get; } = new();

            public void Dispose()
            {
            }

            public void LogException(Exception ex, LogAttributes logAttributes = null)
            {
            }

            public void LogError(string message, LogAttributes logAttributes = null)
            {
            }

            public void LogError(string message, Exception exception, LogAttributes logAttributes = null)
            {
            }

            public void LogWarning(string message, LogAttributes logAttributes = null)
            {
            }

            public void LogInfo(string message, LogAttributes logAttributes = null)
            {
            }

            public void LogDebug(string message, LogAttributes logAttributes = null)
            {
                DebugMessages.Add(message);
            }

            public void LogFormat(LogLevel logLevel, string message)
            {
            }

            public void LogFormat(LogLevel logLevel, string message, LogAttributes logAttributes = null, params object[] args)
            {
            }

            public void LogFormat<T1>(LogLevel level, string message, LogAttributes attrs, in T1 arg1)
            {
            }

            public void LogFormat<T1, T2>(LogLevel level, string message, LogAttributes attrs, in T1 arg1, in T2 arg2)
            {
            }

            public void LogFormat<T1, T2, T3>(LogLevel level, string message, LogAttributes attrs, in T1 arg1, in T2 arg2, in T3 arg3)
            {
            }

            public void LogFormat<T1>(LogLevel level, string message, in T1 arg1)
            {
            }

            public void LogFormat<T1, T2>(LogLevel level, string message, in T1 arg1, in T2 arg2)
            {
            }

            public void LogFormat<T1, T2, T3>(LogLevel level, string message, in T1 arg1, in T2 arg2, in T3 arg3)
            {
            }
        }
    }
}
