using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

namespace TheBestLogger.Examples.LogTargets
{
    public class IMGUIRuntimeLogTarget : LogTarget
    {
        private IMGUIRuntimeDrawer _drawer;
        private IMGUIRuntimeLogTargetConfiguration _configuration;

        private ConcurrentQueue<string> _logEntries = new ConcurrentQueue<string>();

        [Preserve]
        public IMGUIRuntimeLogTarget()
        {
            _drawer = new GameObject(nameof(IMGUIRuntimeDrawer), typeof(IMGUIRuntimeDrawer))
                .GetComponent<IMGUIRuntimeDrawer>();
            UnityEngine.Object.DontDestroyOnLoad(_drawer.gameObject);
        }

        public override void Log(LogLevel level,
                                 string category,
                                 string message,
                                 LogAttributes logAttributes,
                                 Exception exception = null
        )
        {
            var messageFormatted = "";
            messageFormatted = string.Concat("[", logAttributes.TimeStampFormatted, "] ", "[", category, "] ", message);

            if (messageFormatted.Length > _configuration.MaxStringLengthForOneMessage)
            {
                messageFormatted = messageFormatted.Substring(0, _configuration.MaxStringLengthForOneMessage);
            }

            _logEntries.Enqueue(messageFormatted);

            // Keep only the last 100 logs to avoid too much memory usage
            if (_logEntries.Count > (_configuration?.CountLogsToPick ?? 100))
            {
                _logEntries.TryDequeue(out var result);
            }
        }

        public override void LogBatch(IReadOnlyList<(LogLevel level, string category, string message, LogAttributes logAttributes, Exception exception)> logBatch)
        {
            throw new NotImplementedException();
        }

        public override void ApplyConfiguration(LogTargetConfiguration configuration)
        {
            base.ApplyConfiguration(configuration);
            _configuration = configuration as IMGUIRuntimeLogTargetConfiguration;
            if (_drawer != null)
            {
                _drawer.Initialize(_configuration, _logEntries);
            }
        }

        public override string LogTargetConfigurationName => nameof(IMGUIRuntimeLogTargetConfiguration);

        public override void Mute(bool mute)
        {
            base.Mute(mute);
            _drawer.enabled = !mute;
        }

        public override void Dispose()
        {
            base.Dispose();
            if (_drawer != null)
            {
                UnityEngine.Object.Destroy(_drawer.gameObject);
            }
        }
    }
}
