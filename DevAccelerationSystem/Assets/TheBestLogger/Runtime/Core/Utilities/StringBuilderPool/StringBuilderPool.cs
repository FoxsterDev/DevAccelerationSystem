using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;

namespace TheBestLogger.Core.Utilities
{
    public class StringBuilderPool : IDisposable
    {
        private readonly ConcurrentBag<StringBuilder> _pool;
        private readonly int _maxCapacity;
        private bool _disposed;

        public StringBuilderPool(int maxCapacity = 100)
        {
            if (maxCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxCapacity), "Max capacity must be greater than zero.");

            _pool = new ConcurrentBag<StringBuilder>();
            _maxCapacity = maxCapacity;
        }

        /// <summary>
        /// Gets a StringBuilder instance from the pool or creates a new one if the pool is empty.
        /// </summary>
        public StringBuilder Get()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(StringBuilderPool));

            return _pool.TryTake(out var stringBuilder) ? stringBuilder : new StringBuilder(1024);
        }

        /// <summary>
        /// Returns a StringBuilder instance to the pool. Clears its content for reuse.
        /// </summary>
        public void Return(StringBuilder stringBuilder)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(StringBuilderPool));

            if (stringBuilder == null)
                throw new ArgumentNullException(nameof(stringBuilder));

            stringBuilder.Clear();

            // Limit the size of the pool to prevent unbounded growth.
            if (_pool.Count < _maxCapacity)
            {
                _pool.Add(stringBuilder);
            }
        }

        /// <summary>
        /// Releases resources used by the pool.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _pool.Clear();
        }

        /// <summary>
        /// Gets the current diagnostic data about the pool, including per-StringBuilder capacities.
        /// </summary>
        public string GetDiagnostics()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(StringBuilderPool));

            var poolList = _pool.ToList(); // Snapshot for safe enumeration
            var totalCapacity = poolList.Sum(sb => sb.Capacity);
            var averageCapacity = poolList.Count > 0 ? totalCapacity / poolList.Count : 0;

            var stringBuilderDetails = string.Join(", ", poolList.Select(sb => sb.Capacity));
            return $"Pool Count: {_pool.Count}, Total Capacity: {totalCapacity}, Average Capacity: {averageCapacity}, Per-StringBuilder Capacities: [{stringBuilderDetails}]";
        }
    }
}
