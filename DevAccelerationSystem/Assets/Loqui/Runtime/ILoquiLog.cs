using System;

namespace Loqui
{
    public interface ILoquiLog
    {
        void LogWarning(string message);
        void LogError(string message);
        void LogException(Exception exception);
    }
}
