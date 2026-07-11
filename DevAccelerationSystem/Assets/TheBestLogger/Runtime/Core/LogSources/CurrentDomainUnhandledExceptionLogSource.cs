using System;

namespace TheBestLogger
{
    internal class CurrentDomainUnhandledExceptionLogSource : ILogSource
    {
        private  ILogConsumer _logConsumer;

        public CurrentDomainUnhandledExceptionLogSource(ILogConsumer logConsumer)
        {
            _logConsumer = logConsumer;
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var logConsumer = _logConsumer;
            if (logConsumer == null)
            {
                return;
            }

            if (e?.ExceptionObject is Exception exception)
            {
                LogSourceSafety.TryLog(logConsumer,
                                       LogLevel.Exception,
                                       nameof(CurrentDomainUnhandledExceptionLogSource),
                                       string.Empty,
                                       exception);
                return;
            }

            LogSourceSafety.TryLog(logConsumer,
                                   LogLevel.Exception,
                                   nameof(CurrentDomainUnhandledExceptionLogSource),
                                   e?.ExceptionObject?.ToString() ?? "UnhandledExceptionObject is null");
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
            _logConsumer = null;
        }
    }
}
