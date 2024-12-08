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
        public static string Format<T1>(string format, T1 arg1)
        {
#if THEBESTLOGGER_ZSTRING_ENABLED
            return Cysharp.Text.ZString.Format(format, arg1);
#endif
            return string.Format(format, arg1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Format<T1, T2>(string format,
                                            T1 arg1,
                                            T2 arg2)
        {
#if THEBESTLOGGER_ZSTRING_ENABLED
            return Cysharp.Text.ZString.Format(format, arg1, arg2);
#endif
            return string.Format(format, arg1, arg2);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Format<T1, T2, T3>(string format,
                                                T1 arg1,
                                                T2 arg2, T3 arg3)
        {
#if THEBESTLOGGER_ZSTRING_ENABLED
            return Cysharp.Text.ZString.Format(format, arg1, arg2, arg3);
#endif
            return string.Format(format, arg1, arg2, arg3);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Format<T1, T2, T3, T4>(string format,
                                                    T1 arg1,
                                                    T2 arg2, T3 arg3, T4 arg4)
        {
#if THEBESTLOGGER_ZSTRING_ENABLED
            return Cysharp.Text.ZString.Format(format, arg1, arg2, arg3, arg4);
#endif
            return string.Format(format, arg1, arg2, arg3, arg4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Format<T1, T2, T3, T4, T5>(string format,
                                                        T1 arg1,
                                                        T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
#if THEBESTLOGGER_ZSTRING_ENABLED
            return Cysharp.Text.ZString.Format(format, arg1, arg2, arg3, arg4, arg5);
#endif
            return string.Format(format, arg1, arg2, arg3, arg4, arg5);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Concat<T1, T2>(T1 arg1,
                                            T2 arg2
        )
        {
#if THEBESTLOGGER_ZSTRING_ENABLED
            return Cysharp.Text.ZString.Concat(arg1, arg2);
#endif
            return string.Concat(arg1, arg2);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Concat<T1, T2, T3>(T1 arg1,
                                                T2 arg2, T3 arg3
        )
        {
#if THEBESTLOGGER_ZSTRING_ENABLED
            return Cysharp.Text.ZString.Concat(arg1, arg2, arg3);
#endif
            return string.Concat(arg1, arg2, arg3);
        }
        public static string Concat<T1, T2, T3, T4, T5>(T1 arg1,
                                                        T2 arg2,
                                                        T3 arg3,
                                                        T4 arg4,
                                                        T5 arg5)
        {
#if THEBESTLOGGER_ZSTRING_ENABLED
            return Cysharp.Text.ZString.Concat(arg1, arg2, arg3, arg4, arg5);
#endif
            return string.Concat(arg1, arg2, arg3, arg4, arg5);
        }

        public static string Concat<T1, T2, T3, T4>(T1 arg1,
                                                    T2 arg2,
                                                    T3 arg3,
                                                    T4 arg4)
        {
#if THEBESTLOGGER_ZSTRING_ENABLED
            return Cysharp.Text.ZString.Concat(arg1, arg2, arg3, arg4);
#endif
            return string.Concat(arg1, arg2, arg3, arg4);
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
#endif
            return string.Concat(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
        }
    }
}
