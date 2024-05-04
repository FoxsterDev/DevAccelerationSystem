using UnityEngine;

namespace DevAccelerationSystem.Core
{
    public class DefaultUnityLogger : ILogger
    {
        private readonly string _prefix;
        private uint _maxLogsLength;
        private string _logs;

        public DefaultUnityLogger(string prefix, uint maxLogsLength)
        {
            _prefix = prefix;
            _maxLogsLength = maxLogsLength;
            _logs = "";
        }

        private void ConcatMessage(string str)
        {
            _maxLogsLength -= (uint) str.Length;
            if (_maxLogsLength > 0) _logs += str;
        }

        public void Error(object message)
        {
            var str = $"[{_prefix}][Error] {message}\n";
            Debug.LogError(str);

            ConcatMessage(str);
        }

        public void Warning(object message)
        {
            var str = $"[{_prefix}][Warning] {message}\n";
            Debug.LogWarning(str);

            ConcatMessage(str);
        }

        public void Info(object message)
        {
            var str = $"[{_prefix}][Info] {message}\n";
            Debug.Log(str);

            ConcatMessage(str);
        }

        public override string ToString()
        {
            return _logs;
        }
    }
}