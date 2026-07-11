using System;
using System.Collections.Generic;

namespace TheBestLogger.Core.Utilities
{
    public static class LogMessageFormatter
    {
        public static string TryFormat<T1>(string category,
                                           string message,
                                           Exception ex,
                                           in T1 a1)
        {
            try
            {
                return Build(category, StringOperations.Format(message, a1), ex);
            }
            catch (Exception)
            {
                return BuildFormatError(category, message);
            }
        }

        public static string TryFormat<T1, T2>(string category,
                                               string message,
                                               Exception ex,
                                               in T1 a1,
                                               in T2 a2)
        {
            try
            {
                return Build(category, StringOperations.Format(message, a1, a2), ex);
            }
            catch (Exception)
            {
                return BuildFormatError(category, message);
            }
        }

        public static string TryFormat<T1, T2, T3>(string category,
                                                   string message,
                                                   Exception ex,
                                                   in T1 a1,
                                                   in T2 a2,
                                                   in T3 a3)
        {
            try
            {
                return Build(category, StringOperations.Format(message, a1, a2, a3), ex);
            }
            catch (Exception)
            {
                return BuildFormatError(category, message);
            }
        }

        public static string TryFormat(string category,
                                       string message,
                                       Exception ex)
        {
            try
            {
                return Build(category, message, ex);
            }
            catch (Exception)
            {
                return BuildFormatError(category, message);
            }
        }

        private static string Build(string category,
                                    string message,
                                    Exception ex)
        {
            if (ex != null)
            {
                var em = ex.Message ?? string.Empty;
                var formatted = !string.IsNullOrEmpty(category)
                                ? StringOperations.Concat("<", category, "> ", message, " ", ex.GetType().Name, ": ", em)
                                : StringOperations.Concat(message, " ", ex.GetType().Name, ": ", em);
                return formatted;
            }

            return string.IsNullOrEmpty(category)
                       ? message
                       : StringOperations.Concat("<", category, "> ", message);
        }

        public static string TryFormat(string category,
                                       string message,
                                       Exception ex,
                                       params object[] args)
        {
            try
            {
                return FormatWithArgs(category, message, ex, args);
            }
            catch (Exception)
            {
                return BuildFormatError(category, message);
            }
        }

        private static string FormatWithArgs(string category,
                                             string message,
                                             Exception ex,
                                             object[] args)
        {
            // Append exception info if present
            if (ex != null)
            {
                var exceptionMessage = ex.Message ?? string.Empty;
                message = StringOperations.Concat(ex.GetType().Name, ": ", exceptionMessage, message);
            }

            var formattedMessage = message;
            var formatError = false;

            if (!string.IsNullOrEmpty(message) && args != null && args.Length > 0)
            {
                try
                {
                    formattedMessage = args.Length switch
                    {
                        1 => StringOperations.Format(message, args[0]),
                        2 => StringOperations.Format(message, args[0], args[1]),
                        3 => StringOperations.Format(message, args[0], args[1], args[2]),
                        4 => StringOperations.Format(message, args[0], args[1], args[2], args[3]),
                        5 => StringOperations.Format(message, args[0], args[1], args[2], args[3], args[4]),
                        _ => string.Format(message, args)
                    };
                }
                catch (Exception)
                {
                    formatError = true;
                }
            }

            if (formatError)
            {
                return BuildFormatError(category, message);
            }
            else
            {
                return string.IsNullOrEmpty(category)
                           ? formattedMessage
                           : StringOperations.Concat("<", category, "> ", formattedMessage);
            }
        }

        private static string BuildFormatError(string category, string message)
        {
            message ??= string.Empty;
            return string.IsNullOrEmpty(category)
                       ? StringOperations.Concat(message, " => cannot be formatted")
                       : StringOperations.Concat("<", category, "> ", message, " => cannot be formatted");
        }

        public static string ToSimpleNotEscapedJson(this List<KeyValuePair<string, object>> keyValuePairs)
        {
            if (keyValuePairs == null || keyValuePairs.Count < 1)
            {
                return string.Empty;
            }

            using (var sb = StringOperations.CreateStringBuilder(512, false))
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
        }
    }
}
