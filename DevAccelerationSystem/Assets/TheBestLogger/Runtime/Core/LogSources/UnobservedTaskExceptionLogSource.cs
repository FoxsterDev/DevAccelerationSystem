using System;
using System.Threading.Tasks;

namespace TheBestLogger
{
    internal class UnobservedTaskExceptionLogSource : ILogSource
    {
        private  ILogConsumer _logConsumer;
        
        public UnobservedTaskExceptionLogSource(ILogConsumer logConsumer)
        {
            _logConsumer = logConsumer;
            TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            var exception = e.Exception;
            if(exception != null)
            {
                var inner = exception.InnerException ?? exception;
                _logConsumer.LogFormat(LogLevel.Exception, nameof(UnobservedTaskExceptionLogSource), "UnobservedTaskException", inner);
            }
            else
            {
                _logConsumer.LogFormat(LogLevel.Exception, nameof(UnobservedTaskExceptionLogSource),
                    "UnobservedTaskException", e.Exception);
            }

            e.SetObserved(); // Mark the exception as observed to prevent the process from terminating
        }

        public  void Dispose()
        {
            _logConsumer = null;
            TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
        }
    }
}