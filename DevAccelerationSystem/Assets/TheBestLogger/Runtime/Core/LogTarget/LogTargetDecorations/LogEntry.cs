using System;

namespace TheBestLogger
{
    public struct LogEntry
    {
        public LogLevel Level;
        public string Category;
        public string Message;
        public LogAttributes Attributes;
        public Exception Exception;

        public LogEntry(
            LogLevel level,
            string category,
            string message,
            LogAttributes attributes,
            Exception exception)
        {
            Level      = level;
            Category   = category;
            Message    = message;
            Attributes = attributes;
            Exception  = exception;
        }
    }
}
