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
            var logConsumer = _logConsumer;
            if (logConsumer == null)
            {
                return;
            }

            e.SetObserved(); // Mark the exception as observed to prevent the process from terminating

            var exception = e.Exception;
            if (exception != null)
            {
                LogSourceSafety.TryLog(logConsumer,
                                       LogLevel.Exception,
                                       nameof(UnobservedTaskExceptionLogSource),
                                       string.Empty,
                                       exception.Flatten());
            }
            else
            {
                LogSourceSafety.TryLog(logConsumer,
                                       LogLevel.Exception,
                                       nameof(UnobservedTaskExceptionLogSource),
                                       "UnobservedTaskException is null");
            }
        }

        public  void Dispose()
        {
            _logConsumer = null;
            TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
        }
    }
}
