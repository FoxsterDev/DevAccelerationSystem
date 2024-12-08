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
