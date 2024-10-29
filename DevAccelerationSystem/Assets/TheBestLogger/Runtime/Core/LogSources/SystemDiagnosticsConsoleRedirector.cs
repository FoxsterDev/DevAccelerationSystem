using System;
using System.IO;
using System.Text;

namespace TheBestLogger
{
    internal class SystemDiagnosticsConsoleRedirector : TextWriter, ILogSource
    {
        private readonly bool _isError;
        private  ILogConsumer _logConsumer;
        private TextWriter _originalConsoleOutput;
      
        public SystemDiagnosticsConsoleRedirector(ILogConsumer logConsumer, bool isError)
        {
            _isError = isError;
            _logConsumer = logConsumer;
            _originalConsoleOutput = isError ? Console.Error : Console.Out;
        }

        public override void Write(string message)
        {
            var level = _isError
                            ? LogLevel.Error
                            : LogLevel.Debug;
            _logConsumer.LogFormat(level, nameof(SystemDiagnosticsConsoleLogSource), message);
            _originalConsoleOutput.Write(message);
        }

        public override void WriteLine(string message)
        {
            var level = _isError
                            ? LogLevel.Error
                            : LogLevel.Debug;
            _logConsumer.LogFormat(level, nameof(SystemDiagnosticsConsoleLogSource), message);
            _originalConsoleOutput.WriteLine(message);
        }


        public override Encoding Encoding => Encoding.UTF8;

        public new void Dispose()
        {
            _logConsumer = null;
            if (_originalConsoleOutput != null)
            {
                if (!_isError)
                {
                    Console.SetOut(_originalConsoleOutput);
                }
                else
                {
                    Console.SetError(_originalConsoleOutput);
                }

                _originalConsoleOutput = null;
            }
            base.Dispose();
        }
    }
}
