using System.Runtime.CompilerServices;

namespace TheBestLogger.Core.Utilities
{
    public static class StringOperations
    {
        private static StringBuilderPool _stringBuilderPool;
        private static StringBuilderPool SbPool => _stringBuilderPool ??= new StringBuilderPool(5);

        public static string GetDiagnostics()
        {
            return SbPool.GetDiagnostics();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static
#if THEBESTLOGGER_ZSTRING_ENABLED
            Cysharp.Text.Utf8ValueStringBuilder
#else
            TheBestLogger.Core.Utilities.PooledStringBuilder
#endif
            CreateStringBuilder(int preferableSize = 512, bool notNested = true)
        {
#if THEBESTLOGGER_ZSTRING_ENABLED
            return Cysharp.Text.ZString.CreateUtf8StringBuilder(notNested);
#else
            return new TheBestLogger.Core.Utilities.PooledStringBuilder(SbPool);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static
#if THEBESTLOGGER_ZSTRING_ENABLED
            Cysharp.Text.Utf16ValueStringBuilder
#else
            TheBestLogger.Core.Utilities.PooledStringBuilder
#endif
            CreateUtf16StringBuilder(int preferableSize = 512, bool notNested = true)
        {
#if THEBESTLOGGER_ZSTRING_ENABLED
            return Cysharp.Text.ZString.CreateStringBuilder(notNested);
#else
            return new TheBestLogger.Core.Utilities.PooledStringBuilder(SbPool);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Format<T1>(string format, T1 arg1)
        {
            if (string.IsNullOrEmpty(format) || format.IndexOf('{') < 0)
            {
                return format ?? string.Empty;
            }
#if THEBESTLOGGER_ZSTRING_ENABLED
            return Cysharp.Text.ZString.Format(format, arg1);

#else
            return string.Format(format, arg1);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Format<T1, T2>(string format,
                                            T1 arg1,
                                            T2 arg2)
        {
            if (string.IsNullOrEmpty(format) || format.IndexOf('{') < 0)
            {
                return format ?? string.Empty;
            }
#if THEBESTLOGGER_ZSTRING_ENABLED
            return Cysharp.Text.ZString.Format(format, arg1, arg2);
#else
            return string.Format(format, arg1, arg2);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Format<T1, T2, T3>(string format,
                                                T1 arg1,
                                                T2 arg2,
                                                T3 arg3)
        {
            if (string.IsNullOrEmpty(format) || format.IndexOf('{') < 0)
            {
                return format ?? string.Empty;
            }
#if THEBESTLOGGER_ZSTRING_ENABLED
            return Cysharp.Text.ZString.Format(format, arg1, arg2, arg3);
#else
            return string.Format(format, arg1, arg2, arg3);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Format<T1, T2, T3, T4>(string format,
                                                    T1 arg1,
                                                    T2 arg2,
                                                    T3 arg3,
                                                    T4 arg4)
        {
            if (string.IsNullOrEmpty(format) || format.IndexOf('{') < 0)
            {
                return format ?? string.Empty;
            }
#if THEBESTLOGGER_ZSTRING_ENABLED
            return Cysharp.Text.ZString.Format(format, arg1, arg2, arg3, arg4);
#else
            return string.Format(format, arg1, arg2, arg3, arg4);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Format<T1, T2, T3, T4, T5>(string format,
                                                        T1 arg1,
                                                        T2 arg2,
                                                        T3 arg3,
                                                        T4 arg4,
                                                        T5 arg5)
        {
            if (string.IsNullOrEmpty(format) || format.IndexOf('{') < 0)
            {
                return format ?? string.Empty;
            }
#if THEBESTLOGGER_ZSTRING_ENABLED
            return Cysharp.Text.ZString.Format(format, arg1, arg2, arg3, arg4, arg5);
#else
            return string.Format(format, arg1, arg2, arg3, arg4, arg5);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Concat<T1, T2>(T1 arg1,
                                            T2 arg2
        )
        {
#if THEBESTLOGGER_ZSTRING_ENABLED
            return Cysharp.Text.ZString.Concat(arg1, arg2);
#else
            return string.Concat(arg1, arg2);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Concat<T1, T2, T3>(T1 arg1,
                                                T2 arg2,
                                                T3 arg3
        )
        {
#if THEBESTLOGGER_ZSTRING_ENABLED
            return Cysharp.Text.ZString.Concat(arg1, arg2, arg3);
#else
            return string.Concat(arg1, arg2, arg3);
#endif
        }

        public static string Concat<T1, T2, T3, T4, T5>(T1 arg1,
                                                        T2 arg2,
                                                        T3 arg3,
                                                        T4 arg4,
                                                        T5 arg5)
        {
#if THEBESTLOGGER_ZSTRING_ENABLED
            return Cysharp.Text.ZString.Concat(arg1, arg2, arg3, arg4, arg5);

#else
            return string.Concat(arg1, arg2, arg3, arg4, arg5);
#endif
        }

        public static string Concat<T1, T2, T3, T4>(T1 arg1,
                                                    T2 arg2,
                                                    T3 arg3,
                                                    T4 arg4)
        {
#if THEBESTLOGGER_ZSTRING_ENABLED
            return Cysharp.Text.ZString.Concat(arg1, arg2, arg3, arg4);
#else
            return string.Concat(arg1, arg2, arg3, arg4);
#endif
        }

        public static string Concat<T1, T2, T3, T4, T5, T6>(T1 arg1,
                                                            T2 arg2,
                                                            T3 arg3,
                                                            T4 arg4,
                                                            T5 arg5,
                                                            T6 arg6)
        {
#if THEBESTLOGGER_ZSTRING_ENABLED
            return Cysharp.Text.ZString.Concat(arg1, arg2, arg3, arg4, arg5, arg6);
#else
            return string.Concat(arg1, arg2, arg3, arg4, arg5, arg6);
#endif
        }

        public static string Concat<T1, T2, T3, T4, T5, T6, T7>(T1 arg1,
                                                                        T2 arg2,
                                                                        T3 arg3,
                                                                        T4 arg4,
                                                                        T5 arg5,
                                                                        T6 arg6,
                                                                        T7 arg7)
        {
#if THEBESTLOGGER_ZSTRING_ENABLED
            return Cysharp.Text.ZString.Concat(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
#else
            return string.Concat(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
#endif
        }
        public static string Concat<T1, T2, T3, T4, T5, T6, T7, T8>(T1 arg1,
                                                                        T2 arg2,
                                                                        T3 arg3,
                                                                        T4 arg4,
                                                                        T5 arg5,
                                                                        T6 arg6,
                                                                        T7 arg7,
                                                                        T8 arg8)
        {
#if THEBESTLOGGER_ZSTRING_ENABLED
            return Cysharp.Text.ZString.Concat(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
#else
            return string.Concat(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
#endif
        }
        public static string Concat<T1, T2, T3, T4, T5, T6, T7, T8, T9>(T1 arg1,
                                                                        T2 arg2,
                                                                        T3 arg3,
                                                                        T4 arg4,
                                                                        T5 arg5,
                                                                        T6 arg6,
                                                                        T7 arg7,
                                                                        T8 arg8,
                                                                        T9 arg9)
        {
#if THEBESTLOGGER_ZSTRING_ENABLED
            return Cysharp.Text.ZString.Concat(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
#else
            return string.Concat(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
#endif
        }

        public static string Concat<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(T1 arg1,
                                                                             T2 arg2,
                                                                             T3 arg3,
                                                                             T4 arg4,
                                                                             T5 arg5,
                                                                             T6 arg6,
                                                                             T7 arg7,
                                                                             T8 arg8,
                                                                             T9 arg9,
                                                                             T10 arg10)
        {
#if THEBESTLOGGER_ZSTRING_ENABLED
            return Cysharp.Text.ZString.Concat(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
#else
            return string.Concat(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
#endif
        }
    }
}
