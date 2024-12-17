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
        private static string projectFolder => _projectFolder ??= Application.dataPath;

        //config: app datapath, needfileinfo, innerexceptiondepth, filterstrings

        public static string ExtractStackTrace(Exception exception)
        {
            const int maximumInnerExceptionDepth = 5;
            const int skipFrames = 4;

            var needFileInfo = false;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            needFileInfo = true;
#endif

            var stackTrace = string.Empty;

            var size = exception?.StackTrace == null
                           ? 512
                           : exception.StackTrace.Length * 2;

            var sb = StringOperations.CreateStringBuilder(size);

            try
            {
                var deepCount = 0;

                for (; exception != null; exception = exception.InnerException)
                {
                    var str10 = StringOperations.Concat(
                        "InnerException at depth: ", deepCount, ", ", exception.GetType().Name, ": ", exception.Message ?? string.Empty, "\n",
                        exception.StackTrace, "\n");
                    sb.AppendLine(str10);

                    deepCount++;

                    if (deepCount == maximumInnerExceptionDepth)
                    {
                        var str1 = StringOperations.Concat("Reached maximum inner exception depth: ", maximumInnerExceptionDepth, "\n");
                        sb.AppendLine(str1);
                        break;
                    }
                }

                BetterExtractFormattedStackTrace(ref sb, skipFrames, needFileInfo);

                stackTrace = sb.ToString();

                if (needFileInfo)
                {
                    //stackTrace = stackTrace.Replace(Application.dataPath, "");
                }
            }
            finally
            {
                // when use with `ref`, can not use `using`.
                sb.Dispose();
            }

            return stackTrace;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void BetterExtractFormattedStackTrace(ref
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
                                var projectFolder2 = Application.dataPath;
                                if (!string.IsNullOrEmpty(projectFolder2) && str2.StartsWith(projectFolder2, StringComparison.OrdinalIgnoreCase))
                                {
                                    str2 = str2.Substring(projectFolder2.Length);
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
