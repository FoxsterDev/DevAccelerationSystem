using System;

namespace TheBestLogger.Core.Utilities
{
    /// <summary>
    /// Produces a stable, allocation-light grouping key for a captured exception:
    /// an FNV-1a hash of "{ExceptionType}|{first non-framework stack frame}".
    /// Never throws; returns a fixed fallback key on any failure so it is safe to
    /// call on the unhandled-exception and startup paths.
    /// </summary>
    internal static class ExceptionFingerprint
    {
        private const string FallbackKey = "00000000";
        private const int MaxScannedLines = 48;

        public static string Compute(Exception exception)
        {
            if (exception == null)
            {
                return FallbackKey;
            }

            try
            {
                exception = Unwrap(exception);
                var type = exception.GetType();
                var typeName = type.FullName ?? type.Name;
                var frame = TryResolveFaultFrame(exception.StackTrace);
                var seed = string.IsNullOrEmpty(frame)
                               ? typeName
                               : StringOperations.Concat(typeName, "|", frame);
                return Fnv1a(seed);
            }
            catch
            {
                return FallbackKey;
            }
        }

        /// <summary>
        /// Unwraps AggregateException layers to the first meaningful inner exception so the
        /// grouping key reflects the real fault (async/unobserved-Task faults arrive wrapped).
        /// </summary>
        internal static Exception Unwrap(Exception exception)
        {
            var guard = 0;
            while (exception is AggregateException aggregate && aggregate.InnerException != null && guard++ < 8)
            {
                exception = aggregate.InnerException;
            }

            return exception;
        }

        private static string TryResolveFaultFrame(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace))
            {
                return null;
            }

            var position = 0;
            var length = stackTrace.Length;
            var scanned = 0;

            while (position < length && scanned < MaxScannedLines)
            {
                var newLine = stackTrace.IndexOf('\n', position);
                var end = newLine < 0 ? length : newLine;
                var line = stackTrace.Substring(position, end - position).Trim();
                position = end + 1;
                scanned++;

                if (line.Length == 0)
                {
                    continue;
                }

                if (line.StartsWith("at ", StringComparison.Ordinal))
                {
                    line = line.Substring(3);
                }

                if (IsFrameworkFrame(line))
                {
                    continue;
                }

                var paren = line.IndexOf('(');
                if (paren > 0)
                {
                    line = line.Substring(0, paren);
                }

                line = line.Trim();
                if (line.Length > 0)
                {
                    return line;
                }
            }

            return null;
        }

        private static bool IsFrameworkFrame(string line)
        {
            return StartsWithNamespace(line, "System")
                   || StartsWithNamespace(line, "Cysharp")
                   || StartsWithNamespace(line, "UnityEngine")
                   || StartsWithNamespace(line, "UnityEditor")
                   || StartsWithNamespace(line, "Unity")
                   || StartsWithNamespace(line, "Mono")
                   || StartsWithNamespace(line, "Microsoft")
                   || StartsWithNamespace(line, "Newtonsoft");
        }

        private static bool StartsWithNamespace(string value, string ns)
        {
            return value.Length >= ns.Length
                   && value.StartsWith(ns, StringComparison.Ordinal)
                   && (value.Length == ns.Length || value[ns.Length] == '.');
        }

        private static string Fnv1a(string value)
        {
            const uint offsetBasis = 2166136261u;
            const uint prime = 16777619u;

            var hash = offsetBasis;
            for (var i = 0; i < value.Length; i++)
            {
                hash ^= value[i];
                hash *= prime;
            }

            return hash.ToString("X8");
        }
    }
}
