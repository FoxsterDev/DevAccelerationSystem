#if THEBESTLOGGER_UNITASK_ENABLED

using System;
using Cysharp.Threading.Tasks;

namespace TheBestLogger.Examples
{
    public class UniTaskExceptionsExample
    {
        public void ThrowOperationCanceledException()
        {
            FooAsync().Forget();
        }
        public void ThrowHandledExceptionInUniTaskVoid()
        {
            FireAndForgetMethodUniTaskVoid().Forget();
        }
        public void ThrowHandledExceptionInVoid()
        {
            FireAndForgetMethod();
        }
        private async UniTaskVoid FireAndForgetMethodUniTaskVoid()
        {
            // do anything...
            await UniTask.Yield();
            throw new ArgumentNullException("FireAndForgetMethodUniTaskVoid");
        }

        private async void FireAndForgetMethod()
        {
            // do anything...
            await UniTask.Yield();
            throw new ArgumentNullException("FireAndForgetMethod");
        }

        public async UniTask<int> FooAsync()
        {
            await UniTask.Yield();
            throw new OperationCanceledException("FooAsync");
        }
    }
}

#endif