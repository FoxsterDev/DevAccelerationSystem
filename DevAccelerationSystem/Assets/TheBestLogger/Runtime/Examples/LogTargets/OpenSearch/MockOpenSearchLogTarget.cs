using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TheBestLogger.Core.Utilities;
using UnityEngine;

namespace TheBestLogger.Examples.LogTargets
{
    public sealed class MockOpenSearchLogTarget : LogTarget
    {
        private const int MaxRecentPayloads = 6;

        private static readonly object Sync = new();
        private static readonly Queue<string> RecentPayloads = new();

        private static int _capturedPayloadCount;
        private static string _lastApiKeyHint = "none";
        private static string _lastEndpoint = "mock://sample-opensearch/logs";
        private static string _lastIndexName = "thebestlogger-sample-preview";
        private static string _lastPayload = "No mock payload captured yet.";

        private readonly string _deviceModel;
        private readonly Func<OpenSearchLogDTO> _dtoFactory;
        private readonly string _gameVersion;
        private readonly string _os;
        private readonly string _platform;
        private readonly string _uuid;

        private string _apiKeyHint = "none";
        private string _bulkHeaderLine = "{ \"index\" : { \"_index\" : \"thebestlogger-sample-preview\" } }";
        private string _indexName = "thebestlogger-sample-preview";
        private string _openSearchUrl = "mock://sample-opensearch/logs";

        public MockOpenSearchLogTarget(Func<OpenSearchLogDTO> dtoFactory = null)
        {
            _dtoFactory = dtoFactory;
            _gameVersion = Application.version;
            _uuid = SystemInfo.deviceUniqueIdentifier;
            _deviceModel = SystemInfo.deviceModel;
            _os = SystemInfo.operatingSystem;
            _platform = Application.platform.ToString();
        }

        public override string LogTargetConfigurationName => nameof(OpenSearchLogTargetConfiguration);

        public static int CapturedPayloadCount
        {
            get
            {
                lock (Sync)
                {
                    return _capturedPayloadCount;
                }
            }
        }

        public override void ApplyConfiguration(LogTargetConfiguration configuration)
        {
            base.ApplyConfiguration(configuration);
            if (configuration is not OpenSearchLogTargetConfiguration config)
            {
                return;
            }

            var formattedDate = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            _indexName = string.Concat(config.IndexPrefix, formattedDate);
            _bulkHeaderLine = string.Concat("{ \"index\" : { \"_index\" : \"", _indexName, "\" } }");
            _openSearchUrl = string.Concat(config.OpenSearchHostUrl, config.OpenSearchSingleLogMethod);
            _apiKeyHint = BuildApiKeyHint(config.ApiKey);

            lock (Sync)
            {
                _lastEndpoint = _openSearchUrl;
                _lastIndexName = _indexName;
                _lastApiKeyHint = _apiKeyHint;
            }
        }

        public override void LogBatch(IReadOnlyList<LogEntry> logBatch)
        {
            var sb = new OpenSearchPayloadBuilder(notNested: true);
            try
            {
                foreach (var log in logBatch)
                {
                    WriteBulkHeader(ref sb);
                    WriteJson(ref sb,
                              LogLevelToString(log.Level),
                              log.Category,
                              log.Message,
                              log.Attributes?.StackTrace,
                              ResolveSerializedTimestamp(log.Attributes),
                              log.Attributes?.Props.ToSimpleNotEscapedJson(),
                              log.Attributes?.Tags);
                    sb.AppendLine();
                }

                CapturePayload(sb.ToString());
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
            var json = BuildLegacyJson(LogLevelToString(level),
                                       category,
                                       message,
                                       logAttributes?.StackTrace,
                                       ResolveSerializedTimestamp(logAttributes),
                                       logAttributes?.Props.ToSimpleNotEscapedJson(),
                                       logAttributes?.Tags);
            var sb = new OpenSearchPayloadBuilder(notNested: true);
            try
            {
                WriteBulkHeader(ref sb);
                sb.Append(json);
                sb.AppendLine();

                CapturePayload(sb.ToString());
            }
            finally
            {
                sb.Dispose();
            }
        }

        public static void ClearCapturedPayloads()
        {
            lock (Sync)
            {
                _capturedPayloadCount = 0;
                _lastPayload = "No mock payload captured yet.";
                RecentPayloads.Clear();
            }
        }

        public static string GetSummary()
        {
            lock (Sync)
            {
                return $"Captured payloads: {_capturedPayloadCount}\n" +
                       $"Last endpoint: {_lastEndpoint}\n" +
                       $"Last index: {_lastIndexName}\n" +
                       $"API key hint: {_lastApiKeyHint}";
            }
        }

        public static string GetRecentPayloadsPreview()
        {
            lock (Sync)
            {
                if (RecentPayloads.Count == 0)
                {
                    return _lastPayload;
                }

                var entries = RecentPayloads.ToArray();
                var builder = new StringBuilder(entries.Length * 96);
                for (var index = 0; index < entries.Length; index++)
                {
                    builder.Append("Request #");
                    builder.Append(_capturedPayloadCount - entries.Length + index + 1);
                    builder.AppendLine();
                    builder.AppendLine(entries[index]);
                    if (index < entries.Length - 1)
                    {
                        builder.AppendLine("-----");
                    }
                }

                return builder.ToString();
            }
        }

        private void CapturePayload(string payload)
        {
            lock (Sync)
            {
                _capturedPayloadCount++;
                _lastPayload = payload;
                _lastEndpoint = _openSearchUrl;
                _lastIndexName = _indexName;
                _lastApiKeyHint = _apiKeyHint;

                if (RecentPayloads.Count == MaxRecentPayloads)
                {
                    RecentPayloads.Dequeue();
                }

                RecentPayloads.Enqueue(payload);
            }

            Diagnostics.Write($"MockOpenSearchLogTarget captured payload #{CapturedPayloadCount}");
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

        private void WriteJson(ref OpenSearchPayloadBuilder sb,
                               string logLevel,
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
            dto.Category = category;
            dto.Message = message;
            dto.Stacktrace = stackTrace;
            dto.TimeUTC = timestamp;
            dto.Attributes = attributes;
            dto.DebugMode = DebugModeEnabled;
            dto.Tags = tags;

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

        private string BuildLegacyJson(string logLevel,
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
            dto.Category = category;
            dto.Message = message;
            dto.Stacktrace = stackTrace;
            dto.TimeUTC = timestamp;
            dto.Attributes = attributes;
            dto.DebugMode = DebugModeEnabled;
            dto.Tags = tags;
            dto.PrepareForJsonSerialization();
            return JsonUtility.ToJson(dto);
        }

        private void WriteBulkHeader(ref OpenSearchPayloadBuilder sb)
        {
            sb.Append(_bulkHeaderLine);
            sb.AppendLine();
        }

        private static string BuildApiKeyHint(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                return "none";
            }

            if (apiKey.Length <= 4)
            {
                return new string('*', apiKey.Length);
            }

            return $"***{apiKey[^4..]}";
        }

        private static string LogLevelToString(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Debug => "Debug",
                LogLevel.Info => "Info",
                LogLevel.Warning => "Warn",
                LogLevel.Exception => "Error",
                LogLevel.Error => "Error",
                _ => "Error"
            };
        }

        private static bool SupportsManualBatchSerialization(OpenSearchLogDTO dto)
        {
            return dto is IOpenSearchBatchJsonSerializable;
        }
    }
}
