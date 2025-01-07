using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace TheBestLogger.Core.Utilities
{
    internal class StackTraceFormatter
    {
        private readonly string _projectFolder;
        private readonly bool _needFileInfo;
        private readonly int _maximumInnerExceptionDepth;
        private readonly int _skipFrames;
        private bool _utf16ValueStringBuilder;
        private FilterOutStackTraceLineEntry[] _filteringOut;
        private readonly bool _enabled;
        private uint _maxLength;
        private int _filteringOutLength = 0;

        public StackTraceFormatter(string projectFolder,
                                   StackTraceFormatterConfiguration formatterConfiguration)
        {
            _utf16ValueStringBuilder = formatterConfiguration.Utf16ValueStringBuilder;
            _projectFolder = projectFolder;
            _needFileInfo = formatterConfiguration.NeedFileInfo;
            _maximumInnerExceptionDepth = formatterConfiguration.MaximumInnerExceptionDepth;
            _skipFrames = formatterConfiguration.SkipFrames;
            _filteringOut = formatterConfiguration.FilterOutLinesWhen;
            _filteringOutLength = _filteringOut?.Length ?? 0;
            _enabled = formatterConfiguration.Enabled;
            _maxLength = formatterConfiguration.MaxLength;
        }

        public string Extract(Exception exception)
        {
            if (!_enabled)
            {
                return exception?.StackTrace ?? "empty";
            }

            var stackTrace = string.Empty;

            var size = exception?.StackTrace == null
                           ? 512
                           : exception.StackTrace.Length * 2;

            var sb = StringOperations.CreateStringBuilder(size);

            try
            {
                var deepCount = -1;
                var str10 = string.Empty;

                for (; exception != null; exception = exception.InnerException)
                {
                    if (deepCount == _maximumInnerExceptionDepth)
                    {
                        var str1 = StringOperations.Concat("Reached maximum inner exception depth: ", _maximumInnerExceptionDepth, "\n");
                        sb.AppendLine(str1);
                        break;
                    }

                    if (deepCount == -1)
                    {
                        str10 = exception.StackTrace;
                    }
                    else
                    {
                        str10 = StringOperations.Concat(
                            "InnerException at depth: ", deepCount, ", ", exception.GetType().Name, ": ", exception.Message ?? string.Empty, "\n",
                            exception.StackTrace, "\n");
                    }

                    deepCount++;
                    sb.AppendLine(str10);
                }

                BetterExtractFormattedStackTrace(ref sb, _skipFrames, _needFileInfo);

                stackTrace = sb.ToString();

                if (_needFileInfo)
                {
                    stackTrace = stackTrace.Replace(_projectFolder, "");
                }

                if (stackTrace.Length > _maxLength)
                {
                    stackTrace = stackTrace.Substring(0, (int) _maxLength);
                    stackTrace = StringOperations.Concat(stackTrace, "\n--Truncated--");
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
        internal bool IsFilteringTheLine(string namespaceName,
                                        string declaringTypeName,
                                        string methodName)
        {
            var filter = false;
            if (_filteringOutLength < 1)
            {
                return false;
            }

            for (var i = 0; i < _filteringOutLength; i++)
            {
                var item = _filteringOut[i];
                if (item == null)
                {
                    continue;
                }

                // Skip if namespace doesn't match
                if (namespaceName != item.DeclaringTypeNamespace)
                {
                    continue;
                }

                var itemTypeNameEntriesLength = item.TypeNameEntries?.Length ?? 0;

                if (itemTypeNameEntriesLength < 1)
                {
                    filter = true;
                    break;
                }

                for (var j = 0; j < itemTypeNameEntriesLength; j++)
                {
                    var item2 = item.TypeNameEntries[j];
                    if (declaringTypeName != item2.DeclaringTypeName)
                        continue;

                    // If a specific method name is given and it matches the current method, move on
                    if (!string.IsNullOrEmpty(item2.MethodName))
                    {
                        if (methodName == item2.MethodName)
                        {
                            filter = true;
                            break;
                        }

                        continue;
                    }

                    filter = true;
                    break;
                }
            }

            return filter;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BetterExtractFormattedStackTrace(ref
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
                if (method == null)
                {
                    continue;
                }

                var declaringType = method.DeclaringType;
                if (declaringType == null)
                {
                    continue;
                }

                var declaringTypeName = declaringType.Name;
                var namespaceName = declaringType.Namespace;
                var mathodName = method.Name;

                //check filtering
                if (IsFilteringTheLine(namespaceName, declaringTypeName, mathodName))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(namespaceName))
                {
                    sb.Append(namespaceName);
                    sb.Append(".");
                }

                sb.Append(declaringTypeName);
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

                    sb.Append(str2);
                    sb.Append(":");
                    sb.Append(frame.GetFileLineNumber().ToString());
                    sb.Append(")");
                }

                sb.Append("\n");
            }
        }

        public static unsafe string ExtractStackTrace(int bufferMax = 16384)
        {
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
