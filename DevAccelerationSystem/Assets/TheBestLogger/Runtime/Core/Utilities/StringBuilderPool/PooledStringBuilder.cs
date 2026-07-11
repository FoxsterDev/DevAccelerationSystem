using System;
using System.Text;

namespace TheBestLogger.Core.Utilities
{
    public class PooledStringBuilder : IDisposable
    {
        private readonly StringBuilderPool _pool;
        private StringBuilder _stringBuilder;

        internal PooledStringBuilder(StringBuilderPool pool)
        {
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _stringBuilder = _pool.Get();
        }

        public StringBuilder StringBuilder
        {
            get
            {
                if (_stringBuilder == null)
                    throw new ObjectDisposedException(nameof(PooledStringBuilder));

                return _stringBuilder;
            }
        }

        public override string ToString()
        {
            return StringBuilder.ToString();
        }

        public void Append(char value)
        {
            StringBuilder.Append(value);
        }

        public void Append(string value)
        {
            StringBuilder.Append(value);
        }

        public void Append(object value)
        {
            StringBuilder.Append(value);
        }

        public void Append(bool value)
        {
            StringBuilder.Append(value);
        }

        public void Append(int value)
        {
            StringBuilder.Append(value);
        }

        public void Append(uint value)
        {
            StringBuilder.Append(value);
        }

        public void Append(long value)
        {
            StringBuilder.Append(value);
        }

        public void Append(ulong value)
        {
            StringBuilder.Append(value);
        }

        public void Append(float value)
        {
            StringBuilder.Append(value);
        }

        public void Append(double value)
        {
            StringBuilder.Append(value);
        }

        public void Append(ReadOnlySpan<char> value)
        {
            StringBuilder.Append(value);
        }

        public void AppendLine()
        {
            StringBuilder.AppendLine();
        }

        public void AppendLine(string line)
        {
            StringBuilder.AppendLine(line);
        }

        public void Dispose()
        {
            if (_stringBuilder != null)
            {
                _pool.Return(_stringBuilder);
                _stringBuilder = null;
            }
        }
        
        
    }
}
