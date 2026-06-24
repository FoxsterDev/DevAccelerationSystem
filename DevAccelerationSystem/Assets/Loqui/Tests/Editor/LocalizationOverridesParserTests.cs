using Loqui.Remote;
using NUnit.Framework;
using UnityEngine;

namespace Loqui.Tests
{
    [TestFixture]
    public class LocalizationOverridesParserTests
    {
        private const string ValidJson =
            "{\"SchemaVersion\":1,\"PayloadVersion\":\"v1\",\"Languages\":[" +
            "{\"LanguageCode\":\"pt-BR\",\"Entries\":[" +
            "{\"Key\":\"shop.title\",\"Default\":\"Comprar\",\"iOS\":\"\",\"Android\":\"Adquirir\"}]}]}";

        [Test]
        public void JsonUtility_RoundTrips_TheRemoteContract()
        {
            var dto = new LocalizationOverridesDto
            {
                SchemaVersion = 1,
                PayloadVersion = "v1",
                Languages = new[]
                {
                    new LocalizationOverrideLanguageDto
                    {
                        LanguageCode = "pt-BR",
                        Entries = new[]
                        {
                            new LocalizationOverrideEntryDto { Key = "shop.title", Default = "Comprar", Android = "Adquirir" }
                        }
                    }
                }
            };

            var json = JsonUtility.ToJson(dto);
            var parsed = JsonUtility.FromJson<LocalizationOverridesDto>(json);

            Assert.AreEqual(1, parsed.SchemaVersion);
            Assert.AreEqual("v1", parsed.PayloadVersion);
            Assert.AreEqual(1, parsed.Languages.Length);
            Assert.AreEqual("pt-BR", parsed.Languages[0].LanguageCode);
            Assert.AreEqual("shop.title", parsed.Languages[0].Entries[0].Key);
            Assert.AreEqual("Adquirir", parsed.Languages[0].Entries[0].Android);
        }

        [Test]
        public void Parse_AcceptsValidPayload_ViaJsonUtility()
        {
            var result = LocalizationOverridesParser.Parse(ValidJson);

            Assert.IsTrue(result.Accepted, result.RejectionReason);
            Assert.AreEqual(1, result.SchemaVersion);
            Assert.AreEqual(1, result.LanguageCount);
            Assert.AreEqual(1, result.KeyCount);
        }

        [Test]
        public void Parse_RejectsMalformedJson()
        {
            var result = LocalizationOverridesParser.Parse("{ not valid json ");

            Assert.IsFalse(result.Accepted);
        }

        [Test]
        public void Parse_RejectsUnsupportedSchemaVersion()
        {
            var result = LocalizationOverridesParser.Parse("{\"SchemaVersion\":99,\"Languages\":[]}");

            Assert.IsFalse(result.Accepted);
        }

        [Test]
        public void Parse_RejectsEmptyPayload()
        {
            Assert.IsFalse(LocalizationOverridesParser.Parse(null).Accepted);
            Assert.IsFalse(LocalizationOverridesParser.Parse(string.Empty).Accepted);
        }
    }
}
