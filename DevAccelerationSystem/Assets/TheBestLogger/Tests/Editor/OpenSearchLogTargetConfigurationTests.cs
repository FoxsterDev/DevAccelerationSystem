using NUnit.Framework;
using TheBestLogger.Examples.LogTargets;
using UnityEngine;

namespace TheBestLogger.Tests.Editor
{
    [TestFixture]
    public class OpenSearchLogTargetConfigurationTests
    {
        private const string LegacyV1Payload = @"{
  ""Muted"": false,
  ""MinLogLevel"": 3,
  ""OverrideCategories"": [
    { ""Category"": ""LoadingFunnel"", ""MinLevel"": 1 }
  ],
  ""BatchLogs"": { ""Enabled"": true, ""UpdatePeriodMs"": 1000, ""MaxCountLogs"": 20 },
  ""DebugMode"": {
    ""Enabled"": false,
    ""MinLogLevel"": 1,
    ""IDs"": [ ""legacy-debug-id"" ],
    ""OverrideCategories"": [ { ""Category"": ""LoadingFunnel"", ""MinLevel"": 0 } ]
  },
  ""IsThreadSafe"": false,
  ""ShowTimestamp"": false,
  ""OpenSearchHostUrl"": ""https://legacy-v1.example"",
  ""OpenSearchSingleLogMethod"": ""/logs"",
  ""IndexPrefix"": ""legacy-v1-"",
  ""ApiKey"": ""legacy-v1-key""
}";

        private const string LegacyV2Payload = @"{
  ""OpenSearchHostUrl"": ""https://legacy-v2.example"",
  ""OpenSearchSingleLogMethod"": ""/bulk"",
  ""IndexPrefix"": ""legacy-v2-"",
  ""ApiKey"": ""legacy-v2-key"",
  ""Muted"": false,
  ""MinLogLevel"": 2,
  ""OverrideCategories"": [
    { ""Category"": ""Gameplay"", ""MinLevel"": 1 }
  ],
  ""BatchLogs"": { ""Enabled"": true, ""UpdatePeriodMs"": 1000, ""MaxCountLogs"": 20 },
  ""DebugMode"": {
    ""Enabled"": true,
    ""MinLogLevel"": 1,
    ""IDs"": [ ""debug-a"", ""debug-b"" ],
    ""OverrideCategories"": [ { ""Category"": ""Gameplay"", ""MinLevel"": 0 } ]
  },
  ""StackTraces"": [
    { ""Level"": 0, ""Enabled"": false },
    { ""Level"": 1, ""Enabled"": false },
    { ""Level"": 2, ""Enabled"": false },
    { ""Level"": 3, ""Enabled"": true },
    { ""Level"": 4, ""Enabled"": true }
  ],
  ""IsThreadSafe"": false,
  ""DispatchingLogsToMainThread"": {
    ""Enabled"": true,
    ""SingleLogDispatchEnabled"": true,
    ""BatchLogsDispatchEnabled"": true
  }
}";

        [Test]
        public void Merge_OnlyOverridesFieldsProvidedByRemoteConfig()
        {
            var local = new OpenSearchLogTargetConfiguration
            {
                OpenSearchHostUrl = "https://local",
                OpenSearchSingleLogMethod = "/bulk",
                IndexPrefix = "game-logs-",
                ApiKey = "secret-local",
                MinLogLevel = LogLevel.Warning
            };

            var remote = new OpenSearchLogTargetConfiguration
            {
                OpenSearchHostUrl = "https://remote",
                OpenSearchSingleLogMethod = string.Empty,
                IndexPrefix = null,
                ApiKey = string.Empty,
                MinLogLevel = LogLevel.Debug
            };

            local.Merge(remote);

            Assert.That(local.OpenSearchHostUrl, Is.EqualTo("https://remote"));
            Assert.That(local.OpenSearchSingleLogMethod, Is.EqualTo("/bulk"));
            Assert.That(local.IndexPrefix, Is.EqualTo("game-logs-"));
            Assert.That(local.ApiKey, Is.EqualTo("secret-local"));
            Assert.That(local.MinLogLevel, Is.EqualTo(LogLevel.Debug));
        }

        [Test]
        public void Merge_ReplacesApiKeyWhenRemoteConfigProvidesNewValue()
        {
            var local = new OpenSearchLogTargetConfiguration { ApiKey = "old-key" };
            var remote = new OpenSearchLogTargetConfiguration { ApiKey = "new-key" };

            local.Merge(remote);

            Assert.That(local.ApiKey, Is.EqualTo("new-key"));
        }

        [Test]
        public void UnityJson_LegacyV1Payload_DeserializesIntoCurrentConfigWithSafeDefaults()
        {
            var config = JsonUtility.FromJson<OpenSearchLogTargetConfiguration>(LegacyV1Payload);

            Assert.That(config, Is.Not.Null);
            Assert.That(config.OpenSearchHostUrl, Is.EqualTo("https://legacy-v1.example"));
            Assert.That(config.OpenSearchSingleLogMethod, Is.EqualTo("/logs"));
            Assert.That(config.IndexPrefix, Is.EqualTo("legacy-v1-"));
            Assert.That(config.ApiKey, Is.EqualTo("legacy-v1-key"));
            Assert.That(config.BatchLogs.MaxCountLogs, Is.EqualTo(20));
            Assert.That(config.DebugMode, Is.Not.Null);
            Assert.That(config.DebugMode.IDs, Is.Not.Null);
            Assert.That(config.DebugMode.IDs, Is.EquivalentTo(new[] { "legacy-debug-id" }));
            Assert.That(config.StackTraces, Is.Not.Null);
            Assert.That(config.StackTraces.Length, Is.EqualTo(5));
            Assert.That(config.DispatchingLogsToMainThread.Enabled, Is.False);
        }

        [Test]
        public void UnityJson_LegacyV2Payload_DeserializesIntoCurrentConfigWithDispatchingFlags()
        {
            var config = JsonUtility.FromJson<OpenSearchLogTargetConfiguration>(LegacyV2Payload);

            Assert.That(config, Is.Not.Null);
            Assert.That(config.OpenSearchHostUrl, Is.EqualTo("https://legacy-v2.example"));
            Assert.That(config.ApiKey, Is.EqualTo("legacy-v2-key"));
            Assert.That(config.BatchLogs.Enabled, Is.True);
            Assert.That(config.BatchLogs.MaxCountLogs, Is.EqualTo(20));
            Assert.That(config.DebugMode, Is.Not.Null);
            Assert.That(config.DebugMode.Enabled, Is.True);
            Assert.That(config.DebugMode.IDs, Is.EquivalentTo(new[] { "debug-a", "debug-b" }));
            Assert.That(config.StackTraces, Is.Not.Null);
            Assert.That(config.StackTraces.Length, Is.EqualTo(5));
            Assert.That(config.DispatchingLogsToMainThread.Enabled, Is.True);
            Assert.That(config.DispatchingLogsToMainThread.SingleLogDispatchEnabled, Is.True);
            Assert.That(config.DispatchingLogsToMainThread.BatchLogsDispatchEnabled, Is.True);
        }

        [Test]
        public void UnityJson_CurrentConfigSerialization_ContainsCriticalFieldNames()
        {
            var config = new OpenSearchLogTargetConfiguration
            {
                OpenSearchHostUrl = "https://current.example",
                OpenSearchSingleLogMethod = "/bulk",
                IndexPrefix = "current-",
                ApiKey = "current-key",
                BatchLogs = new LogTargetBatchLogsConfiguration
                {
                    Enabled = true,
                    UpdatePeriodMs = 1000,
                    MaxCountLogs = 20
                },
                DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration
                {
                    Enabled = true,
                    SingleLogDispatchEnabled = true,
                    BatchLogsDispatchEnabled = true
                }
            };

            var json = JsonUtility.ToJson(config);

            StringAssert.Contains("\"OpenSearchHostUrl\"", json);
            StringAssert.Contains("\"OpenSearchSingleLogMethod\"", json);
            StringAssert.Contains("\"IndexPrefix\"", json);
            StringAssert.Contains("\"ApiKey\"", json);
            StringAssert.Contains("\"DispatchingLogsToMainThread\"", json);
            StringAssert.Contains("\"SingleLogDispatchEnabled\"", json);
            StringAssert.Contains("\"BatchLogsDispatchEnabled\"", json);
        }
    }
}
