#if LOGGER_UNITASK_ENABLED

using System;
using Cysharp.Threading.Tasks;

namespace TheBestLogger.Examples
{
    public class UniTaskException
    {
        public void ThrowHandledExceptionInAsyncVoid()
        {
            FireAndForgetMethod().Forget();
        }
        private async UniTaskVoid FireAndForgetMethod()
        {
            // do anything...
            await UniTask.Yield();
            throw new ArgumentNullException();
        }

    }
}

#endif