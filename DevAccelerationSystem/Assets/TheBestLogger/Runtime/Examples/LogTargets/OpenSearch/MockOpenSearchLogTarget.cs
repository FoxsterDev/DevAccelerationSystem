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
        private readonly string _gameVersion;
        private readonly string _os;
        private readonly string _platform;
        private readonly string _uuid;

        private string _apiKeyHint = "none";
        private string _indexName = "thebestlogger-sample-preview";
        private string _openSearchUrl = "mock://sample-opensearch/logs";

        public MockOpenSearchLogTarget()
        {
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
            using var sb = StringOperations.CreateStringBuilder();
            foreach (var log in logBatch)
            {
                sb.AppendLine($"{{ \"index\" : {{ \"_index\" : \"{_indexName}\" }} }}");
                sb.AppendLine(BuildJson(log.Level,
                                        log.Category,
                                        log.Message,
                                        log.Attributes?.StackTrace,
                                        log.Attributes?.TimeStampFormatted,
                                        log.Attributes?.Props.ToSimpleNotEscapedJson(),
                                        log.Attributes?.Tags));
            }

            CapturePayload(sb.ToString());
        }

        public override void Log(LogLevel level,
                                 string category,
                                 string message,
                                 LogAttributes logAttributes,
                                 Exception exception = null)
        {
            using var sb = StringOperations.CreateStringBuilder();
            sb.AppendLine($"{{ \"index\" : {{ \"_index\" : \"{_indexName}\" }} }}");
            sb.AppendLine(BuildJson(level,
                                    category,
                                    message,
                                    logAttributes?.StackTrace,
                                    logAttributes?.TimeStampFormatted,
                                    logAttributes?.Props.ToSimpleNotEscapedJson(),
                                    logAttributes?.Tags));

            CapturePayload(sb.ToString());
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

        private string BuildJson(LogLevel level,
                                 string category,
                                 string message,
                                 string stackTrace,
                                 string timestamp,
                                 string attributes,
                                 string[] tags)
        {
            var dto = new OpenSearchLogDTO
            {
                GameVersion = _gameVersion,
                UUID = _uuid,
                DeviceModel = _deviceModel,
                OS = _os,
                Platform = _platform,
                LogLevel = LogLevelToString(level),
                Category = category,
                Message = message,
                Stacktrace = stackTrace,
                TimeUTC = timestamp,
                Attributes = attributes,
                DebugMode = DebugModeEnabled,
                Tags = tags
            };

            return JsonUtility.ToJson(dto);
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
    }
}
