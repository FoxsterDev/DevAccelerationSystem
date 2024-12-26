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
            e.SetObserved(); // Mark the exception as observed to prevent the process from terminating

            var exception = e.Exception;
            if(exception != null)
            {
                var inner = exception.InnerException ?? exception;
                _logConsumer.LogFormat(LogLevel.Exception, nameof(UnobservedTaskExceptionLogSource), string.Empty, inner);
            }
            else
            {
                _logConsumer.LogFormat(LogLevel.Exception, nameof(UnobservedTaskExceptionLogSource),
                    "UnobservedTaskException is null", null);
            }
        }

        public  void Dispose()
        {
            _logConsumer = null;
            TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
        }
    }
}