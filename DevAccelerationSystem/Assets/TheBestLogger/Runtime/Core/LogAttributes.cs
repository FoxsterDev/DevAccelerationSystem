using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;

namespace TheBestLogger
{
    public class LogAttributes
    {
        private const int DEFAULT_CAPACITY_PROPS = 3;

        private readonly LogImportance _logImportance = LogImportance.NiceToHave;

        public LogImportance LogImportance => _logImportance;

        public List<KeyValuePair<string, object>> Props;

        public string StackTrace;
        public string TimeStampFormatted;
        public DateTime TimeUtc;

        public Object UnityContextObject;

        [Preserve]
        public LogAttributes(LogImportance logImportance)
        {
            _logImportance = logImportance;
        }

        [Preserve]
        public LogAttributes Add(string key, object value)
        {
            Props ??= new List<KeyValuePair<string, object>>(DEFAULT_CAPACITY_PROPS);
            Props.Add(new KeyValuePair<string, object>(key, value));
            return this;
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
    }
}
