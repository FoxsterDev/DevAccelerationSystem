using System;
using Cysharp.Text;

public static class StackTraceFormatter
{
    public static string ExtractStackTraceFromException(this Exception exception) 
    {
        var stackTrace = (exception as Exception)?.StackTrace ?? string.Empty;
        stackTrace = EscapeStackTrace(stackTrace);
        return stackTrace;
    }
    
    public static string ExtractStringFromException(object exception)
    {
        var stackTrace = exception != null ? UnityEngine.StackTraceUtility.ExtractStringFromException(exception) : string.Empty;
        stackTrace = EscapeStackTrace(stackTrace);
        return stackTrace;
    }
        
    public static string EscapeStackTrace(string stacktrace)
    {
        // Escape the special characters for JSON
        return stacktrace
            .Replace("\\", "\\\\") // Escape backslashes
            .Replace("\"", "\\\"")  // Escape double quotes
            .Replace("\n", "\\n")   // Escape newline characters
            .Replace("\r", "\\r");  // Escape carriage return characters
    }
    
    public static string EscapeStackTrace2(string stacktrace)
    {
        if (string.IsNullOrEmpty(stacktrace))
            return stacktrace; // Return the original string if it's null or empty

        var escapedStacktrace = new Utf16ValueStringBuilder(true);
        try
        {
            foreach (char c in stacktrace)
            {
                switch (c)
                {
                    case '\\': // Escape backslash
                        escapedStacktrace.Append("\\\\");
                        break;
                    case '\"': // Escape double quote
                        escapedStacktrace.Append("\\\"");
                        break;
                    case '\n': // Escape newline
                        escapedStacktrace.Append("\\n");
                        break;
                    case '\r': // Escape carriage return
                        escapedStacktrace.Append("\\r");
                        break;
                    default:
                        escapedStacktrace.Append(c); // Append the character as is
                        break;
                }
            }
            return escapedStacktrace.ToString();
        }
        finally
        {
            escapedStacktrace.Dispose();
        }
    }
}