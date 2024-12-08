using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace TheBestLogger.Core.Utilities
{
    public static class StackTraceFormatter
    {
        private static string _projectFolder = "";
        private static  string projectFolder => _projectFolder ??= Application.dataPath;

        public static string ExtractStackTrace(Exception exception)
        {
            var needFileInfo = false;

#if UNITY_EDITOR
            needFileInfo = true;
#else
            needFileInfo = UnityEngine.Debug.isDebugBuild;
#endif

            needFileInfo = true;

            string stackTrace;
            string exceptionMessage;

            BetterExtractStringFromExceptionInternal(exception, out exceptionMessage, out stackTrace, 3, 5, needFileInfo);

            return stackTrace;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BetterExtractStringFromExceptionInternal(
            Exception exception,
            out string exceptionMessage,
            out string stackTrace,
            int maximumInnerExceptionDepth,
            int skipFrames,
            bool needFileInfo)
        {
            exceptionMessage = string.Empty;
            stackTrace = string.Empty;

            var size = exception?.StackTrace == null
                           ? 512
                           : exception.StackTrace.Length * 2;

            var sb = StringOperations.CreateStringBuilder(size);

            try
            {
                var str1 = string.Empty;
                var deepCount = 0;
                for (; exception != null; exception = exception.InnerException)
                {
                    if (needFileInfo)
                    {
                        str1 = string.IsNullOrEmpty(str1) 
                                   ? exception.StackTrace 
                                   : StringOperations.Concat(exception.StackTrace,"\n", str1);
                    }

                    var str2 = string.Empty;
                    if (deepCount > 0)
                    {
                        str2 = exception.GetType().Name;

                        var str3 = string.Empty;
                        if (exception.Message != null)
                        {
                            str3 = exception.Message;
                        }

                        if (str3.Trim().Length != 0)
                        {
                            str2 = StringOperations.Concat(str2,": ",str3);
                        }

                        exceptionMessage = str2;
                    }

                    if (exception.InnerException != null)
                    {
                        str1 = StringOperations.Concat("Rethrow as " ,str2 ,"\n" ,str1);
                    }

                    deepCount++;

                    if (deepCount == maximumInnerExceptionDepth)
                    {
                        str1 = StringOperations.Concat(str1, "Reached maximum inner exception depth\n");
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(str1))
                {
                    sb.AppendLine(str1);
                }

                BetterExtractFormattedStackTrace(ref sb, skipFrames, needFileInfo);

                stackTrace = sb.ToString();
            }
            finally
            {
                // when use with `ref`, can not use `using`.
                sb.Dispose();
            }
        }

        public static void BetterExtractFormattedStackTrace(ref
#if THEBESTLOGGER_ZSTRING_ENABLED
                                                                 Cysharp.Text.Utf8ValueStringBuilder
#else
            TheBestLogger.Core.Utilities.PooledStringBuilder
#endif
                                                                 sb,
                                                             int skipFrames,
                                                             bool needFileInfo)
        {
            var stackTrace = new StackTrace(skipFrames, needFileInfo);

            for (var index1 = 0; index1 < stackTrace.FrameCount; ++index1)
            {
                var frame = stackTrace.GetFrame(index1);
                var method = frame.GetMethod();
                if (method != null)
                {
                    var declaringType = method.DeclaringType;
                    if (declaringType != null)
                    {
                        var str1 = declaringType.Namespace;
                        if (!string.IsNullOrEmpty(str1))
                        {
                            sb.Append(str1);
                            sb.Append(".");
                        }

                        sb.Append(declaringType.Name);
                        sb.Append(":");
                        sb.Append(method.Name);
                        sb.Append("(");
                        var index2 = 0;
                        var parameters = method.GetParameters();
                        var flag = true;
                        for (; index2 < parameters.Length; ++index2)
                        {
                            if (!flag)
                            {
                                sb.Append(", ");
                            }
                            else
                            {
                                flag = false;
                            }

                            sb.Append(parameters[index2].ParameterType.Name);
                        }

                        sb.Append(")");

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
                                  declaringType.Namespace == "TheBestLogger" ||
                                  (declaringType.Namespace == "UnityEngine.Assertions" && declaringType.Name == "Assert") ||
                                  (declaringType.Namespace == "UnityEngine" && declaringType.Name == "MonoBehaviour" && method.Name == "print")))
                            {
                                sb.Append(" (at ");
                                if (!string.IsNullOrEmpty(projectFolder) && str2.StartsWith(projectFolder, StringComparison.OrdinalIgnoreCase))
                                {
                                    str2 = str2.Substring(projectFolder.Length);
                                    str2 = str2.TrimStart(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
                                }

                                sb.Append(str2);
                                sb.Append(":");
                                sb.Append(frame.GetFileLineNumber().ToString());
                                sb.Append(")");
                            }
                        }

                        sb.Append("\n");
                    }
                }
            }
        }

        public static unsafe string ExtractStackTrace(int bufferMax = 16384)
        {
            //return new StackTrace(false).ToString();
            var buffer = stackalloc byte[bufferMax];
            var stackTraceNoAlloc = UnityEngine.Debug.ExtractStackTraceNoAlloc(buffer, bufferMax, "");

            return stackTraceNoAlloc > 0
                       ? new string((sbyte*) buffer, 0, stackTraceNoAlloc, Encoding.UTF8)
                       : string.Empty;
        }

        public static string EscapeStackTrace2(string stacktrace)
        {
            if (string.IsNullOrEmpty(stacktrace))
            {
                return stacktrace; // Return the original string if it's null or empty
            }

            using (var escapedStacktrace = StringOperations.CreateStringBuilder())
            {
                foreach (var c in stacktrace)
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
        }
    }
}

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

//private static string _projectFolder = "";

/*[RequiredByNativeCode]
internal static void SetProjectFolder(string folder)
{
    StackTraceUtility.projectFolder = folder;
    if (string.IsNullOrEmpty(StackTraceUtility.projectFolder))
        return;
    StackTraceUtility.projectFolder = StackTraceUtility.projectFolder.Replace("\\", "/");
}*/


        /*public static void ExtractStringFromExceptionInternal(
            Exception exception,
            out string exceptionMessage,
            out string stackTrace,
            int maximumInnerExceptionDepth,
            int skipFrames,
            bool needFileInfo)
        {
            exceptionMessage = string.Empty;
            stackTrace = string.Empty;

            var deepCount = 0;

            var sb = new StringBuilder(
                exception?.StackTrace == null
                    ? 512
                    : exception.StackTrace.Length * 2);

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

                    var str3 = string.Empty;
                    if (exception.Message != null)
                    {
                        str3 = exception.Message;
                    }

                    if (str3.Trim().Length != 0)
                    {
                        str2 = str2 + ": " + str3;
                    }

                    exceptionMessage = str2;
                }

                if (exception.InnerException != null)
                {
                    str1 = "Rethrow as " + str2 + "\n" + str1;
                }

                deepCount++;

                if (deepCount == maximumInnerExceptionDepth)
                {
                    str1 += "Reached maximum inner exception depth\n";
                    break;
                }
            }

            if (!string.IsNullOrEmpty(str1))
            {
                sb.AppendLine(str1);
            }

            sb.Append(ExtractFormattedStackTrace(skipFrames, needFileInfo));
            stackTrace = sb.ToString();
        }*/

       /* public static string ExtractFormattedStackTrace(int skipFrames, bool needFileInfo)
        {
            var stackTrace = new StackTrace(skipFrames, needFileInfo);

            var stringBuilder = new StringBuilder(byte.MaxValue);
            for (var index1 = 0; index1 < stackTrace.FrameCount; ++index1)
            {
                var frame = stackTrace.GetFrame(index1);
                var method = frame.GetMethod();
                if (method != null)
                {
                    var declaringType = method.DeclaringType;
                    if (declaringType != null)
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
                        var index2 = 0;
                        var parameters = method.GetParameters();
                        var flag = true;
                        for (; index2 < parameters.Length; ++index2)
                        {
                            if (!flag)
                            {
                                stringBuilder.Append(", ");
                            }
                            else
                            {
                                flag = false;
                            }

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
                                  declaringType.Namespace == "TheBestLogger" ||
                                  (declaringType.Namespace == "UnityEngine.Assertions" && declaringType.Name == "Assert") ||
                                  (declaringType.Namespace == "UnityEngine" && declaringType.Name == "MonoBehaviour" && method.Name == "print")))
                            {
                                stringBuilder.Append(" (at ");
                                if (!string.IsNullOrEmpty(projectFolder) && str2.Replace("\\", "/").StartsWith(projectFolder))
                                {
                                    str2 = str2.Substring(projectFolder.Length, str2.Length - projectFolder.Length);
                                }

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
        }*/
