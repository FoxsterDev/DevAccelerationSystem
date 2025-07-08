using System;
using System.Collections.Generic;
using TheBestLogger.Core.Utilities;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;

namespace TheBestLogger
{
    public class LogAttributes
    {
        private const int DEFAULT_CAPACITY_PROPS = 3;

        private readonly LogImportance _logImportance = LogImportance.NiceToHave;

        public List<KeyValuePair<string, object>> Props;

        public string StackTrace;
        public string[] Tags = Array.Empty<string>();
        public string TimeStampFormatted;
        public DateTime TimeUtc;
        public Object UnityContextObject;

        [Preserve]
        public LogAttributes(LogImportance logImportance)
        {
            _logImportance = logImportance;
        }

        [Preserve]
        public LogAttributes()
        {
        }

        [Preserve]
        public LogAttributes(int capacityProps)
        {
            Props = new List<KeyValuePair<string, object>>(capacityProps);
        }

        [Preserve]
        public LogAttributes(string key, object value)
        {
            Props = new List<KeyValuePair<string, object>>(DEFAULT_CAPACITY_PROPS)
            {
                new(key, value)
            };
        }

        [Preserve]
        public LogAttributes(string stackTrace)
        {
            StackTrace = stackTrace;
        }

        [Preserve]
        public LogAttributes(Object obj)
        {
            UnityContextObject = obj;
        }

        public LogImportance LogImportance => _logImportance;

        [Preserve]
        public LogAttributes Add(string key, object value)
        {
            Props ??= new List<KeyValuePair<string, object>>(DEFAULT_CAPACITY_PROPS);
            Props.Add(new KeyValuePair<string, object>(key, value));
            return this;
        }

        public override string ToString()
        {
            using var sb = StringOperations.CreateStringBuilder(512, false);
            sb.AppendLine("\n[LogAttributes]");
            sb.AppendLine($"  Importance: {LogImportance}");

            if (Tags != null && Tags.Length > 0)
            {
                sb.AppendLine($"  Tags: {string.Join(", ", Tags)}");
            }

            if (Props != null && Props.Count > 0)
            {
                sb.AppendLine("  Props:");
                foreach (var kvp in Props)
                {
                    sb.AppendLine($"    - {kvp.Key}: {kvp.Value}");
                }
            }

            if (!string.IsNullOrEmpty(StackTrace))
            {
                sb.AppendLine("  StackTrace: (trimmed)\n" + StackTrace.Split('\n')[0] + " ...");
            }

            if (UnityContextObject != null)
            {
                sb.AppendLine($"  Context: {UnityContextObject.name} ({UnityContextObject.GetType().Name})");
            }

            return sb.ToString();
        }
    }
}
