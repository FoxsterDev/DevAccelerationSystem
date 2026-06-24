using System;
using System.Collections.Generic;
using ILogger = TheBestLogger.ILogger;

namespace Loqui
{
    public sealed class LocalizationEvent
    {
        private readonly List<Action> _listeners = new();
        private readonly Dictionary<Action, int> _index = new();
        private Action[] _raiseBuffer = Array.Empty<Action>();

        public int Count => _listeners.Count;

        public void Add(Action listener)
        {
            if (listener == null || _index.ContainsKey(listener))
            {
                return;
            }

            _index[listener] = _listeners.Count;
            _listeners.Add(listener);
        }

        public void Remove(Action listener)
        {
            if (listener == null || !_index.TryGetValue(listener, out var i))
            {
                return;
            }

            var last = _listeners.Count - 1;
            var moved = _listeners[last];
            _listeners[i] = moved;
            _listeners.RemoveAt(last);
            _index[moved] = i;
            _index.Remove(listener);
        }

        public void Clear()
        {
            _listeners.Clear();
            _index.Clear();
        }

        public void Raise(ILogger logger = null)
        {
            var count = _listeners.Count;
            if (count == 0)
            {
                return;
            }

            if (_raiseBuffer.Length < count)
            {
                _raiseBuffer = new Action[count];
            }

            _listeners.CopyTo(0, _raiseBuffer, 0, count);
            for (var i = 0; i < count; i++)
            {
                var listener = _raiseBuffer[i];
                _raiseBuffer[i] = null;
                try
                {
                    listener();
                }
                catch (Exception ex)
                {
                    logger?.LogException(ex);
                }
            }
        }
    }
}
