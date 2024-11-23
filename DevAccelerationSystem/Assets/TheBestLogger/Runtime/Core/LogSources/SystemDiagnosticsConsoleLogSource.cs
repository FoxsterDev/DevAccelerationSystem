using System;

namespace TheBestLogger
{
    /// <summary>
    /// This approach allows you to easily catch and display errors from third-party DLLs or other code that uses Console.Error for logging errors, so they appear in the Unity console for debugging.
    ///Console.WriteLine or Console.Write writes output to the console or standard output stream.It is part of the System namespace.
    ///The output can be seen in a terminal, console application window, or wherever the standard output stream (stdout) is redirected (e.g., a log file).
    ///It works in both Debug and Release configurations.
    ///This method is generally used for displaying information intended for end-users or simple logging during the development of console applications.
    /// </summary>
    internal class SystemDiagnosticsConsoleLogSource :  ILogSource
    {
        private SystemDiagnosticsConsoleRedirector _debugConsoleRedirector;
        private SystemDiagnosticsConsoleRedirector _errorConsoleRedirector;
        public SystemDiagnosticsConsoleLogSource(ILogConsumer logConsumer)
        {
            _debugConsoleRedirector = new SystemDiagnosticsConsoleRedirector(logConsumer, false);
            Console.SetOut(_debugConsoleRedirector);

            _errorConsoleRedirector = new SystemDiagnosticsConsoleRedirector(logConsumer, true);
            Console.SetError(_errorConsoleRedirector);
        }

        public void Dispose()
        {
            _debugConsoleRedirector.Dispose();
            _errorConsoleRedirector.Dispose();
        }
    }
}
