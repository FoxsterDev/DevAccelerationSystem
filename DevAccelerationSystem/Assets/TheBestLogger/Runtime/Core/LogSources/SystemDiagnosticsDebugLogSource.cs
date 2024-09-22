using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace TheBestLogger
{
    internal class SystemDiagnosticsDebugLogSource : TraceListener, ILogSource
    {
        private  ILogConsumer _logConsumer;

        public SystemDiagnosticsDebugLogSource(ILogConsumer logConsumer)
        {
            _logConsumer = logConsumer;
        }

        public override void Write(string message)
        {
            _logConsumer.LogFormat(LogLevel.Debug, nameof(SystemDiagnosticsDebugLogSource), message);
        }

        public override void WriteLine(string message)
        {
            _logConsumer.LogFormat(LogLevel.Debug, nameof(SystemDiagnosticsDebugLogSource), message);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _logConsumer = null;
        }
    }
}