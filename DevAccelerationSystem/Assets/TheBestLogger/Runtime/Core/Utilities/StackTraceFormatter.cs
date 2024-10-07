using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Cysharp.Text;
using UnityEngine;

namespace TheBestLogger.Core.Utilities
{
    public static class StackTraceFormatter
    {
        /*public static string ExtractStackTraceFromException(this Exception exception,
                                                            bool escapeStackTrace = false,
                                                            bool unityApproach = true)
        {
            string stackTrace;
            if (unityApproach)
            {
                stackTrace = exception != null
                                 ? ExtractStringFromException(exception)
                                 : string.Empty;
            }
            else
            {
                stackTrace = (exception as Exception)?.StackTrace ?? string.Empty;
            }

            if (escapeStackTrace)
            {
                stackTrace = EscapeStackTrace2(stackTrace);
            }

            return stackTrace;
        }*/

        private static string _projectFolder = "";

        /*[RequiredByNativeCode]
        internal static void SetProjectFolder(string folder)
        {
            StackTraceUtility.projectFolder = folder;
            if (string.IsNullOrEmpty(StackTraceUtility.projectFolder))
                return;
            StackTraceUtility.projectFolder = StackTraceUtility.projectFolder.Replace("\\", "/");
        }*/
        private static string projectFolder = "";

        public static void ExtractStringFromExceptionInternal(
            Exception exception,
            out string exceptionMessage,
            out string stackTrace,
            int maximumInnerExceptionDepth,
            int skipFrames,
            bool needFileInfo)
        {
            if (exception == null)
                throw new ArgumentException("ExtractStringFromExceptionInternal called with null exception");

            var deepCount = 0;

            var stringBuilder = new StringBuilder(
                exception.StackTrace == null
                    ? 512
                    : exception.StackTrace.Length * 2);

            exceptionMessage = string.Empty;
            var str1 = string.Empty;

            for (; exception != null; exception = exception.InnerException)
            {
                if (needFileInfo)
                {
                    str1 = str1.Length != 0
                               ? exception.StackTrace + "\n" + str1
                               : exception.StackTrace;
                }

                var str2 = string.Empty;
                if (deepCount > 0)
                {
                    str2 = exception.GetType().Name;
                    /*if (exception.Source != null)
                    {
                        str2 += ": "+exception.Source+" ";
                    }*/
                    
                    var str3 = string.Empty;
                    if (exception.Message != null)
                        str3 = exception.Message;
                    if (str3.Trim().Length != 0)
                        str2 = str2 + ": " + str3;
                    exceptionMessage = str2;
                }

                if (exception.InnerException != null)
                    str1 = "Rethrow as " + str2 + "\n" + str1;

                deepCount++;

                if (deepCount == maximumInnerExceptionDepth)
                {
                    str1 += "Reached maximum inner exception depth\n";
                    break;
                }
            }

            stringBuilder.AppendLine(str1);

            stringBuilder.Append(ExtractFormattedStackTrace(skipFrames, needFileInfo));
            stackTrace = stringBuilder.ToString();
        }

        public static string ExtractFormattedStackTrace(int skipFrames, bool needFileInfo)
        {
            var stackTrace = new StackTrace(skipFrames, needFileInfo);

            var stringBuilder = new StringBuilder((int) byte.MaxValue);
            for (int index1 = 0; index1 < stackTrace.FrameCount; ++index1)
            {
                var frame = stackTrace.GetFrame(index1);
                var method = frame.GetMethod();
                if (method != null)
                {
                    var declaringType = method.DeclaringType;
                    if (declaringType != (Type) null)
                    {
                        var str1 = declaringType.Namespace;
                        if (!string.IsNullOrEmpty(str1))
                        {
                            stringBuilder.Append(str1);
                            stringBuilder.Append(".");
                        }

                        stringBuilder.Append(declaringType.Name);
                        stringBuilder.Append(":");
                        stringBuilder.Append(method.Name);
                        stringBuilder.Append("(");
                        int index2 = 0;
                        var parameters = method.GetParameters();
                        bool flag = true;
                        for (; index2 < parameters.Length; ++index2)
                        {
                            if (!flag)
                                stringBuilder.Append(", ");
                            else
                                flag = false;
                            stringBuilder.Append(parameters[index2].ParameterType.Name);
                        }

                        stringBuilder.Append(")");

                        // (at /Users/Projects/Logger/Assets/Scripts/EntryPoint.cs:48) - debug
                        //[0x00000] in <00000000000000000000000000000000>:0  release
                        //EntryPoint.MethodInner2 () (at Assets/Scripts/EntryPoint.cs:65) unity logmessage received
                        if (needFileInfo)
                        {
                            var str2 = frame.GetFileName();

                            if (str2 != null &&
                                !((declaringType.Namespace == "UnityEngine" &&
                                   (declaringType.Name == "Debug" ||
                                    declaringType.Name == "Logger" ||
                                    declaringType.Name == "DebugLogHandler" ||
                                    declaringType.Name == "StackTraceUtility")) ||
                                  (declaringType.Namespace == "TheBestLogger") ||
                                  (declaringType.Namespace == "UnityEngine.Assertions" && declaringType.Name == "Assert") ||
                                  (declaringType.Namespace == "UnityEngine" && declaringType.Name == "MonoBehaviour" && method.Name == "print")))
                            {
                                stringBuilder.Append(" (at ");
                                if (!string.IsNullOrEmpty(projectFolder) && str2.Replace("\\", "/").StartsWith(projectFolder))
                                    str2 = str2.Substring(projectFolder.Length, str2.Length - projectFolder.Length);

                                stringBuilder.Append(str2);
                                stringBuilder.Append(":");
                                stringBuilder.Append(frame.GetFileLineNumber().ToString());
                                stringBuilder.Append(")");
                            }
                        }

                        stringBuilder.Append("\n");
                    }
                }
            }

            return stringBuilder.ToString();
        }

        public static unsafe string ExtractStackTrace(int bufferMax = 16384)
        {
            //return new StackTrace(false).ToString();
            byte* buffer = stackalloc byte[bufferMax];
            var stackTraceNoAlloc = UnityEngine.Debug.ExtractStackTraceNoAlloc(buffer, bufferMax, "");

            return stackTraceNoAlloc > 0
                       ? new string((sbyte*) buffer, 0, stackTraceNoAlloc, Encoding.UTF8)
                       : string.Empty;
        }

        /*public static string ExtractStringFromException(object exception)
        {
           
        }*/

        /*public static string EscapeStackTrace(string stacktrace)
        {
            // Escape the special characters for JSON
            return stacktrace
                .Replace("\\", "\\\\") // Escape backslashes
                .Replace("\"", "\\\"")  // Escape double quotes
                .Replace("\n", "\\n")   // Escape newline characters
                .Replace("\r", "\\r");  // Escape carriage return characters
        }*/

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
}
