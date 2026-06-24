using System.Text;
using UnityEngine;
using ILogger = TheBestLogger.ILogger;

namespace Loqui.Remote
{
    public static class LocalizationOverridesParser
    {
        public const int SupportedSchemaVersion = 1;
        public const int MaxPayloadBytes = 256 * 1024;

        public static LocalizationOverridesResult Parse(string json, ILogger logger = null)
        {
            if (string.IsNullOrEmpty(json))
            {
                return LocalizationOverridesResult.Reject("Empty payload.");
            }

            if (Encoding.UTF8.GetByteCount(json) > MaxPayloadBytes)
            {
                logger?.LogWarning("[Localization] Remote overrides payload exceeds max size; rejected.");
                return LocalizationOverridesResult.Reject("Payload above max size.");
            }

            LocalizationOverridesDto dto;
            try
            {
                dto = JsonUtility.FromJson<LocalizationOverridesDto>(json);
            }
            catch
            {
                logger?.LogWarning("[Localization] Remote overrides payload is malformed JSON; rejected.");
                return LocalizationOverridesResult.Reject("Malformed JSON.");
            }

            if (dto == null)
            {
                return LocalizationOverridesResult.Reject("Malformed JSON.");
            }

            if (dto.SchemaVersion != SupportedSchemaVersion)
            {
                return LocalizationOverridesResult.Reject($"Unsupported schema version {dto.SchemaVersion}.");
            }

            if (dto.Languages == null || dto.Languages.Length == 0)
            {
                return LocalizationOverridesResult.Reject("No languages in payload.");
            }

            var languageCount = 0;
            var keyCount = 0;
            foreach (var language in dto.Languages)
            {
                if (language == null || string.IsNullOrEmpty(language.LanguageCode) || language.Entries == null || language.Entries.Length == 0)
                {
                    return LocalizationOverridesResult.Reject("Empty language block.");
                }

                languageCount++;
                keyCount += language.Entries.Length;
            }

            return new LocalizationOverridesResult
            {
                Accepted = true,
                SchemaVersion = dto.SchemaVersion,
                PayloadVersion = dto.PayloadVersion,
                LanguageCount = languageCount,
                KeyCount = keyCount,
                Payload = dto
            };
        }
    }
}
