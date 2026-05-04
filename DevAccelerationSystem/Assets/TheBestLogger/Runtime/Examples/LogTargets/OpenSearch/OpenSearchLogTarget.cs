using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TheBestLogger.Core.Utilities;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Scripting;

namespace TheBestLogger.Examples.LogTargets
{
    public class OpenSearchLogTarget : LogTarget
    {
        private static readonly UTF8Encoding Utf8NoBom = new(false);

        private readonly string _deviceModel;

        private readonly Func<OpenSearchLogDTO> _dtoFactory;

        private readonly string _gameVersion;
        private readonly string _os;
        private readonly string _platform;
        private readonly string _uuid;
        private string _apiKey;
        private string _bulkHeaderLine;
        private string _indexName;
        private string _openSearchUrl;

        [Preserve]
        public OpenSearchLogTarget(Func<OpenSearchLogDTO> dtoFactory = null)
        {
            _dtoFactory = dtoFactory;
            _gameVersion = Application.version;
            _uuid = SystemInfo.deviceUniqueIdentifier;
            _deviceModel = SystemInfo.deviceModel;
            _os = SystemInfo.operatingSystem;
            _platform = Application.platform.ToString();
        }

        public override string LogTargetConfigurationName => nameof(OpenSearchLogTargetConfiguration);

        public override void ApplyConfiguration(LogTargetConfiguration configuration)
        {
            base.ApplyConfiguration(configuration);
            if (configuration is OpenSearchLogTargetConfiguration config)
            {
                _apiKey = config.ApiKey;
                var utcNow = DateTime.UtcNow;
                var formattedDate = utcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                _indexName = StringOperations.Concat(config.IndexPrefix, formattedDate);
                _bulkHeaderLine = StringOperations.Concat("{ \"index\" : { \"_index\" : \"", _indexName, "\" } }");

                _openSearchUrl =
                    StringOperations.Concat(config.OpenSearchHostUrl, config.OpenSearchSingleLogMethod);
            }
        }

        public override void LogBatch(
            IReadOnlyList<LogEntry> logBatch)
        {
            var sb = new OpenSearchPayloadBuilder(notNested: true);
            try
            {
                foreach (var log in logBatch)
                {
                    WriteBulkHeader(ref sb);
                    WriteLogDataJson(ref sb,
                                     LogLevelToString(log.Level),
                                     log.Category,
                                     log.Message,
                                     log.Attributes?.StackTrace,
                                     ResolveSerializedTimestamp(log.Attributes),
                                     log.Attributes?.Props.ToSimpleNotEscapedJson(),
                                     log.Attributes?.Tags);
                    sb.AppendLine();
                }

                PostLog(_openSearchUrl, sb.ToUtf8Bytes());
            }
            finally
            {
                sb.Dispose();
            }
        }

        public override void Log(LogLevel level,
                                 string category,
                                 string message,
                                 LogAttributes logAttributes,
                                 Exception exception = null)
        {
            var dto = CreateDto(LogLevelToString(level),
                                category,
                                message,
                                logAttributes?.StackTrace,
                                ResolveSerializedTimestamp(logAttributes),
                                logAttributes?.Props.ToSimpleNotEscapedJson(),
                                logAttributes?.Tags);
            dto.PrepareForJsonSerialization();

            var json = JsonUtility.ToJson(dto);
            var sb = new OpenSearchPayloadBuilder(notNested: true);
            try
            {
                WriteBulkHeader(ref sb);
                sb.Append(json);
                sb.AppendLine();

                PostLog(_openSearchUrl, sb.ToUtf8Bytes());
            }
            finally
            {
                sb.Dispose();
            }
        }

        private OpenSearchLogDTO CreateDto(string logLevel,
                                           string category,
                                           string message,
                                           string stackTrace,
                                           string timestamp,
                                           string attributes,
                                           string[] tags)
        {
            var dto = _dtoFactory != null
                          ? _dtoFactory.Invoke()
                          : new DefaultOpenSearchBatchCompatibleLogDTO();
            dto.GameVersion = _gameVersion;
            dto.UUID = _uuid;
            dto.DeviceModel = _deviceModel;
            dto.OS = _os;
            dto.Platform = _platform;
            dto.LogLevel = logLevel;
            dto.Message = message;
            dto.Stacktrace = stackTrace;
            dto.TimeUTC = timestamp;
            dto.Attributes = attributes;
            dto.Category = category;
            dto.DebugMode = DebugModeEnabled;
            dto.Tags = tags;
            return dto;
        }

        private void WriteLogDataJson(ref OpenSearchPayloadBuilder sb,
                                      string logLevel,
                                      string category,
                                      string message,
                                      string stackTrace,
                                      string timestamp,
                                      string attributes,
                                      string[] tags)
        {
            var dto = CreateDto(logLevel, category, message, stackTrace, timestamp, attributes, tags);
            dto.PrepareForJsonSerialization();
            if (SupportsManualBatchSerialization(dto))
            {
                ((IOpenSearchBatchJsonSerializable)dto).WriteJson(ref sb);
            }
            else
            {
                sb.Append(JsonUtility.ToJson(dto));
            }
        }

        private static bool SupportsManualBatchSerialization(OpenSearchLogDTO dto)
        {
            return dto is IOpenSearchBatchJsonSerializable;
        }

        private static string ResolveSerializedTimestamp(LogAttributes logAttributes)
        {
            if (logAttributes == null)
            {
                return string.Empty;
            }

            if (logAttributes.TimeUtc != default)
            {
                return OpenSearchTimestampFormatter.FormatUtc(logAttributes.TimeUtc);
            }

            if (!string.IsNullOrEmpty(logAttributes.TimeStampFormatted))
            {
                return logAttributes.TimeStampFormatted;
            }

            return string.Empty;
        }

        private void PostLog(string url, byte[] jsonData)
        {
            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);

            request.SetRequestHeader("x-api-key", _apiKey);
            request.uploadHandler = new UploadHandlerRaw(jsonData);

#if UNITY_EDITOR || THEBESTLOGGER_DIAGNOSTICS_ENABLED
            request.downloadHandler = new DownloadHandlerBuffer();
#else
            request.downloadHandler = null; // Do not process the response body
#endif
            request.SetRequestHeader("Content-Type", "application/json");

            var async = request.SendWebRequest();
            async.completed += operation =>
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
#if UNITY_EDITOR || THEBESTLOGGER_DIAGNOSTICS_ENABLED
                    var payloadString = Utf8NoBom.GetString(jsonData, 0, jsonData.Length);
                    var messageError = $"Can not write log into opensearchtarget because error result: {request.result}, error: {request.error}, response: {request.downloadHandler?.text}\nsent:{payloadString}";
#if UNITY_EDITOR
                    ReflectiveUnityEditorConsoleLogger.LogToConsoleDirectly(messageError, LogType.Error);
#endif
                    Diagnostics.Write(messageError);
#endif
                }

                request.Dispose();
            };
        }

        private void WriteBulkHeader(ref OpenSearchPayloadBuilder sb)
        {
            sb.Append(_bulkHeaderLine);
            sb.AppendLine();
        }

        private static string LogLevelToString(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Debug:
                {
                    return "Debug";
                }
                case LogLevel.Info:
                {
                    return "Info";
                }
                case LogLevel.Warning:
                {
                    return "Warn";
                }
                case LogLevel.Exception:
                {
                    return "Error";
                }
                case LogLevel.Error:
                {
                    return "Error";
                }
            }

            return "Error";
        }
    }

    public static class OpenSearchTimestampFormatter
    {
        public static string FormatUtc(DateTime timeUtc)
        {
            return string.Create(24, timeUtc, static (span, value) =>
            {
                Write4Digits(span, 0, value.Year);
                span[4] = '-';
                Write2Digits(span, 5, value.Month);
                span[7] = '-';
                Write2Digits(span, 8, value.Day);
                span[10] = 'T';
                Write2Digits(span, 11, value.Hour);
                span[13] = ':';
                Write2Digits(span, 14, value.Minute);
                span[16] = ':';
                Write2Digits(span, 17, value.Second);
                span[19] = '.';
                Write3Digits(span, 20, value.Millisecond);
                span[23] = 'Z';
            });
        }

        private static void Write2Digits(Span<char> span, int start, int value)
        {
            span[start] = (char)('0' + (value / 10));
            span[start + 1] = (char)('0' + (value % 10));
        }

        private static void Write3Digits(Span<char> span, int start, int value)
        {
            span[start] = (char)('0' + (value / 100));
            span[start + 1] = (char)('0' + ((value / 10) % 10));
            span[start + 2] = (char)('0' + (value % 10));
        }

        private static void Write4Digits(Span<char> span, int start, int value)
        {
            span[start] = (char)('0' + (value / 1000));
            span[start + 1] = (char)('0' + ((value / 100) % 10));
            span[start + 2] = (char)('0' + ((value / 10) % 10));
            span[start + 3] = (char)('0' + (value % 10));
        }
    }
}
