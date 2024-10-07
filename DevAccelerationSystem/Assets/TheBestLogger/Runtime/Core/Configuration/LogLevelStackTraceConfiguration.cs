using System.Collections.Generic;

namespace TheBestLogger
{
    [System.Serializable]
    public class LogLevelStackTraceConfiguration : IEnumerable<LogLevelStackTraceConfigurationEntry>
    {
        public LogLevelStackTraceConfigurationEntry LogLevelDebug =   new LogLevelStackTraceConfigurationEntry { Level = LogLevel.Debug, Enabled = false };
        public LogLevelStackTraceConfigurationEntry LogLevelInfo =   new LogLevelStackTraceConfigurationEntry { Level = LogLevel.Info, Enabled = false };
        public LogLevelStackTraceConfigurationEntry LogLevelWarning =   new LogLevelStackTraceConfigurationEntry { Level = LogLevel.Warning, Enabled = false };
        public LogLevelStackTraceConfigurationEntry LogLevelError =   new LogLevelStackTraceConfigurationEntry { Level = LogLevel.Error, Enabled = true };
        public LogLevelStackTraceConfigurationEntry LogLevelException =   new LogLevelStackTraceConfigurationEntry { Level = LogLevel.Exception, Enabled = true };

        public bool IsEnabled(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Debug : return LogLevelDebug.Enabled;
                case LogLevel.Info:  return LogLevelInfo.Enabled;
                case LogLevel.Warning:  return LogLevelWarning.Enabled;
                case LogLevel.Error:  return LogLevelError.Enabled;
                case LogLevel.Exception:  return LogLevelException.Enabled;
            }
            return false;
        }

        public IEnumerator<LogLevelStackTraceConfigurationEntry> GetEnumerator()
        {
            yield return LogLevelDebug;
            yield return LogLevelInfo;
            yield return LogLevelWarning;
            yield return LogLevelError;
            yield return LogLevelException;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
