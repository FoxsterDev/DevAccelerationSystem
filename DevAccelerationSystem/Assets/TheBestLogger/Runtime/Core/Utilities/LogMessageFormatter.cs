using System;
using System.Collections.Generic;
using Cysharp.Text;

namespace TheBestLogger
{
    public static class LogMessageFormatter
    {
        public static string TryFormat(string category, string message, params object[] args)
        {
            var formatError = false;
            var formattedMessage = message;
            
            if (args != null && args.Length > 0 && !string.IsNullOrEmpty(message))
            {
                formattedMessage = args.Length switch
                {
                    1 => ZString.Format(message, args[0]),
                    2 => ZString.Format(message, args[0], args[1]),
                    3 => ZString.Format(message, args[0], args[1], args[2]),
                    4 => ZString.Format(message, args[0], args[1], args[2], args[3]),
                    5 => ZString.Format(message, args[0], args[1], args[2], args[3], args[4]),
                    _ => string.Format(message, args)
                };

                formatError = string.IsNullOrEmpty(formattedMessage);
            }

            if (formatError)
            {
                //build str from args ?
                message = ZString.Concat("[",category, "] ", message, " =>can not be formatted");
            }
            else
            {
                message = ZString.Concat("[",category, "] ", formattedMessage);
            }
            
            return message;
        }
        public static string TryFormat(string message, params object[] args)
        {
            var formatError = false;
            var formattedMessage = message;
            
            if (args != null && args.Length > 0 && !string.IsNullOrEmpty(message))
            {
                formattedMessage = args.Length switch
                {
                    1 => ZString.Format(message, args[0]),
                    2 => ZString.Format(message, args[0], args[1]),
                    3 => ZString.Format(message, args[0], args[1], args[2]),
                    4 => ZString.Format(message, args[0], args[1], args[2], args[3]),
                    5 => ZString.Format(message, args[0], args[1], args[2], args[3], args[4]),
                    _ => string.Format(message, args)
                };

                formatError = string.IsNullOrEmpty(formattedMessage);
            }
            
            return formattedMessage;
        }

        public static string ToSimpleNotEscapedJson(this List<KeyValuePair<string, object>> keyValuePairs)
        {
            if (keyValuePairs == null || keyValuePairs.Count < 1) return string.Empty;
            
            var sb = new Utf16ValueStringBuilder(true);
            try
            {
                sb.Append('{');
                var count = keyValuePairs.Count;
                for (var i = 0; i < count; i++)
                {
                    var kvp = keyValuePairs[i];
                    sb.Append('\"');
                    sb.Append(kvp.Key);
                    sb.Append("\":");

                    if (kvp.Value is string)
                    {
                        sb.Append('\"');
                        sb.Append(kvp.Value.ToString());
                        sb.Append('\"');
                    }
                    else
                    {
                        sb.Append(kvp.Value);
                    }

                    if (i < keyValuePairs.Count - 1)
                    {
                        sb.Append(',');
                    }
                }

                sb.Append('}');
                return sb.ToString();
            }
            finally
            {
                sb.Dispose();
            }
            
           

        }

        public static string TryFormat(string category, string message, Exception ex = null, string stackTrace = null, LogAttributes logAttributes = null, params object[] args)
        {
            string formattedMessage = null;
            if (args != null && args.Length > 0)
            {
                //ZString.Format does not throw exceptions
                formattedMessage = args.Length switch
                {
                    1 => ZString.Format(message, args[0]),
                    2 => ZString.Format(message, args[0], args[1]),
                    3 => ZString.Format(message, args[0], args[1], args[2]),
                    4 => ZString.Format(message, args[0], args[1], args[2], args[3]),
                    5 => ZString.Format(message, args[0], args[1], args[2], args[3], args[4]),
                    _ => string.Format(message, args)
                };
            }

            if (string.IsNullOrEmpty(formattedMessage))
            {
                formattedMessage = ZString.Concat(message, " =>can not be formatted");
            }

            string logContextStr = string.Empty;
            
            if (logAttributes != null)
            {
                logContextStr = $"{ZString.Join(Environment.NewLine, logAttributes.Props)}{Environment.NewLine}";
            }

            string logMessage = string.Empty;
            
            if (ex == null)
            {
                logMessage = ZString.Format("[{0}] {1}{2}{3}", category, formattedMessage, Environment.NewLine, logContextStr);
            }
            else
            {
                logMessage = ZString.Format("[{0}] {1}{2}{3}\n{4}\n", category, formattedMessage, Environment.NewLine, logContextStr, ex);
            }


            return logMessage;
        }
    }
}

/*
     *   if (logContext != null)
                 {
                     propsMessage = $"{ZString.Join(Environment.NewLine, logContext)}{Environment.NewLine}";
                 }

                 if (exception == null)
                 {
                     logMessage = ZString.Format("[{0}] {1}{2}{3}", category, message, Environment.NewLine, propsMessage);
                 }
                 else
                 {
                     logMessage = ZString.Format("[{0}] {1}{2}{3}\n{4}\n", category, message, Environment.NewLine, propsMessage, exception);
                 }
     */