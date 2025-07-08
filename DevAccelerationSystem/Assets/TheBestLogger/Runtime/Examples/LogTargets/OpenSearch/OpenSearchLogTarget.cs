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
        private readonly string _deviceModel;

        private readonly Func<OpenSearchLogDTO> _dtoFactory;

        private readonly string _gameVersion;
        private readonly string _os;
        private readonly string _platform;
        private readonly string _uuid;
        private string _apiKey;
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

                _openSearchUrl =
                    StringOperations.Concat(config.OpenSearchHostUrl, config.OpenSearchSingleLogMethod);
            }
        }

        public override void LogBatch(
            IReadOnlyList<LogEntry> logBatch)
        {
            var send = "";
            var sb = StringOperations.CreateStringBuilder();
            try
            {
                foreach (var log in logBatch)
                {
                    var logLevel = LogLevelToString(log.Level);
                    var logDataJson = LogDataToJson(
                        logLevel, log.Category, log.Message, log.Attributes.StackTrace,
                        log.Attributes.TimeStampFormatted,
                        log.Attributes.Props.ToSimpleNotEscapedJson(), log.Attributes.Tags);

                    sb.AppendLine(StringOperations.Format("{{ \"index\" : {{ \"_index\" : \"{0}\" }} }}", _indexName));
                    sb.AppendLine(logDataJson);
                }

                send = sb.ToString();
            }
            finally
            {
                // when use with `ref`, can not use `using`.
                sb.Dispose();
            }

            PostLog(_openSearchUrl, send);
        }

        public override void Log(LogLevel level,
                                 string category,
                                 string message,
                                 LogAttributes logAttributes,
                                 Exception exception = null)
        {
            var logLevel = LogLevelToString(level);
            var json = LogDataToJson(
                logLevel, category, message, logAttributes.StackTrace,
                logAttributes.TimeStampFormatted,
                logAttributes.Props.ToSimpleNotEscapedJson(), logAttributes.Tags);
            var send = "";
            using (var sb = StringOperations.CreateStringBuilder())
            {
                sb.AppendLine(StringOperations.Format("{{ \"index\" : {{ \"_index\" : \"{0}\" }} }}", _indexName));
                sb.AppendLine(json);

                send = sb.ToString();
            }

            PostLog(_openSearchUrl, send);
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
                          : new OpenSearchLogDTO();
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
            dto.DebugMode = Configuration.DebugMode.Enabled;
            dto.Tags = tags;
            return dto;
        }

        private string LogDataToJson(string logLevel,
                                     string category,
                                     string message,
                                     string stackTrace,
                                     string timestamp,
                                     string attributes,
                                     string[] tags)
        {
            var dto = CreateDto(logLevel, category, message, stackTrace, timestamp, attributes, tags);
            var jsonString = JsonUtility.ToJson(dto);

            return jsonString;
        }

        private void PostLog(string url, string jsonData)
        {
            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);

            request.SetRequestHeader("x-api-key", _apiKey);
            var jsonToSend = new UTF8Encoding().GetBytes(jsonData);

            request.uploadHandler = new UploadHandlerRaw(jsonToSend);

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
                    var messageError = $"Can not write log into opensearchtarget because error result: {request.result}, error: {request.error}, response: {request.downloadHandler?.text}\nsent:{jsonData}";
                    ReflectiveUnityEditorConsoleLogger.LogToConsoleDirectly(messageError, LogType.Error);
                    Diagnostics.Write(messageError);
#endif
                }

                request.Dispose();
            };
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
}
