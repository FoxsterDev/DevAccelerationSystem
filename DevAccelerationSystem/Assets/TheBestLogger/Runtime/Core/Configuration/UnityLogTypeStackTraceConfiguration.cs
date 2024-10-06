using System.Collections.Generic;
using UnityEngine;

namespace TheBestLogger
{
    [System.Serializable]
    public class UnityLogTypeStackTraceConfiguration : IEnumerable<UnityLogTypeStackTraceConfigurationEntry>
    {
        public UnityLogTypeStackTraceConfigurationEntry LogTypeLog = new UnityLogTypeStackTraceConfigurationEntry
            { LogType = LogType.Log, StackTraceLevel = StackTraceLogType.None };
        public UnityLogTypeStackTraceConfigurationEntry LogTypeWarning = new UnityLogTypeStackTraceConfigurationEntry
            { LogType = LogType.Warning, StackTraceLevel = StackTraceLogType.None };
        public UnityLogTypeStackTraceConfigurationEntry LogTypeError = new UnityLogTypeStackTraceConfigurationEntry
            { LogType = LogType.Error, StackTraceLevel = StackTraceLogType.ScriptOnly };
        public UnityLogTypeStackTraceConfigurationEntry LogTypeException = new UnityLogTypeStackTraceConfigurationEntry
            { LogType = LogType.Exception, StackTraceLevel = StackTraceLogType.Full };
        public UnityLogTypeStackTraceConfigurationEntry LogTypeAssert = new UnityLogTypeStackTraceConfigurationEntry
            { LogType = LogType.Assert, StackTraceLevel = StackTraceLogType.ScriptOnly };

        public IEnumerator<UnityLogTypeStackTraceConfigurationEntry> GetEnumerator()
        {
            yield return LogTypeLog;
            yield return LogTypeWarning;
            yield return LogTypeError;
            yield return LogTypeException;
            yield return LogTypeAssert;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
