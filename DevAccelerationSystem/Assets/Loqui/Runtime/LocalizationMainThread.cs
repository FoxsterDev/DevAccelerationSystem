using System.Diagnostics;
using System.Threading;

namespace Loqui
{
    internal static class LocalizationMainThread
    {
        private static int _mainThreadId;
        private static bool _captured;

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Capture()
        {
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            _captured = true;
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Verify(string member, ILoquiLog logger)
        {
            if (_captured && Thread.CurrentThread.ManagedThreadId != _mainThreadId)
            {
                logger?.LogError($"[Localization] {member} must be called on the Unity main thread.");
            }
        }
    }
}
