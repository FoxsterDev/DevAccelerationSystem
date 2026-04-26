using NUnit.Framework;
using TheBestLogger.Examples.LogTargets;

namespace TheBestLogger.Tests.Editor
{
    [TestFixture]
    public class OpenSearchLogTargetConfigurationTests
    {
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
    }
}
