using System;
using System.Collections.Generic;

namespace Loqui
{
    public sealed class LocalizationEvent
    {
        private readonly List<Action> _listeners = new();
        private readonly Dictionary<Action, int> _index = new();
        private Action[] _raiseBuffer = Array.Empty<Action>();
        private bool _raising;

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

        public void Raise(ILoquiLog logger = null)
        {
            var count = _listeners.Count;
            if (count == 0)
            {
                return;
            }

            Action[] buffer;
            var reusingShared = !_raising;
            if (reusingShared)
            {
                if (_raiseBuffer.Length < count)
                {
                    _raiseBuffer = new Action[count];
                }

                buffer = _raiseBuffer;
                _raising = true;
            }
            else
            {
                buffer = new Action[count];
            }

            _listeners.CopyTo(0, buffer, 0, count);
            try
            {
                for (var i = 0; i < count; i++)
                {
                    var listener = buffer[i];
                    buffer[i] = null;
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
            finally
            {
                if (reusingShared)
                {
                    _raising = false;
                }
            }
        }
    }
}
