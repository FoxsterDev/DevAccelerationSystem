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
            if (_logConsumer == null)
            {
                return;
            }

            if (e?.ExceptionObject is Exception exception)
            {
                _logConsumer.LogFormat(LogLevel.Exception, nameof(CurrentDomainUnhandledExceptionLogSource), string.Empty, exception);
                return;
            }

            _logConsumer.LogFormat(LogLevel.Exception,
                                   nameof(CurrentDomainUnhandledExceptionLogSource),
                                   e?.ExceptionObject?.ToString() ?? "UnhandledExceptionObject is null",
                                   null);
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
            _logConsumer = null;
        }
    }
}
