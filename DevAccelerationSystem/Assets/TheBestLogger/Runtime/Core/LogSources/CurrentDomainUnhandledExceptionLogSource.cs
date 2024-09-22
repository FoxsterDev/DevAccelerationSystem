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
            _logConsumer?.LogFormat(LogLevel.Exception, nameof(CurrentDomainUnhandledExceptionLogSource), "UnhandledException" , (Exception)e.ExceptionObject);
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
            _logConsumer = null;
        }
    }
}