using System;

namespace TheBestLogger
{
    internal class UnobservedUniTaskExceptionLogSource : ILogSource
    {
        private ILogConsumer _logConsumer;

        public UnobservedUniTaskExceptionLogSource(ILogConsumer logConsumer, UniTaskConfiguration uniTaskConfiguration)
        {
            _logConsumer = logConsumer;

#if THEBESTLOGGER_UNITASK_ENABLED
            Cysharp.Threading.Tasks.UniTaskScheduler.DispatchUnityMainThread = uniTaskConfiguration.DispatchUnityMainThread;
            Cysharp.Threading.Tasks.UniTaskScheduler.PropagateOperationCanceledException = uniTaskConfiguration.PropagateOperationCanceledException;
            Cysharp.Threading.Tasks.UniTaskScheduler.UnobservedExceptionWriteLogType = uniTaskConfiguration.UnobservedExceptionWriteLogType;
            //
            Cysharp.Threading.Tasks.UniTaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
            Cysharp.Threading.Tasks.UniTaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
#else
            Diagnostics.Write("UniTask are not included to the project. Skipped adding lof source!");
#endif
        }

        private void OnUnobservedTaskException(Exception obj)
        {
            if (obj != null)
            {
                _logConsumer.LogFormat(LogLevel.Exception, nameof(UnobservedUniTaskExceptionLogSource), string.Empty, obj);
            }
            else
            {
                _logConsumer.LogFormat(
                    LogLevel.Exception, nameof(UnobservedUniTaskExceptionLogSource),
                    "UnobservedUniTaskException is null", null);
            }
        }

        public void Dispose()
        {
            _logConsumer = null;
#if THEBESTLOGGER_UNITASK_ENABLED
            Cysharp.Threading.Tasks.UniTaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
#endif
        }
    }
}
