using System;
using System.Threading;

namespace TheBestLogger.Core.Utilities
{
    internal class UtilitySupplier 
    {
        private readonly int _mainThreadId;
        public bool IsMainThread => _mainThreadId == Thread.CurrentThread.ManagedThreadId;
        private readonly long _timezoneOffsetTicks;
        private long _lastIssuedUtcTicks;
        private readonly uint _minTimestampPeriodMs;
        private DateTime LocalTime => new DateTime(DateTime.UtcNow.Ticks + _timezoneOffsetTicks, DateTimeKind.Local);

        private CachedTimestampData _cachedTimestampData;

        public UtilitySupplier(uint minTimestampPeriodMs, StackTraceFormatter stackTraceFormatter)
        {
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;

            _minTimestampPeriodMs = minTimestampPeriodMs;
            _lastIssuedUtcTicks = DateTime.UtcNow.Ticks;
            _cachedTimestampData = new CachedTimestampData(DateTime.MinValue, string.Empty);
            TagsRegistry = new ConcurrentTagsRegistry(2, 4);
//_timezoneOffsetTicks = (DateTime.Now - DateTime.UtcNow).Ticks;
            StackTraceFormatter = stackTraceFormatter;
        }

        public (DateTime currentTimeUtc, string timeStampFormatted) GetTimeStamp()
        {
            var timeUtc = ReserveUniqueTimeUtc();
            var cachedTimestampData = _cachedTimestampData;

            if ((timeUtc - cachedTimestampData.CachedUtc).TotalMilliseconds >= _minTimestampPeriodMs)
            {
                var newTimeStampString = timeUtc.ToString(
                    "yyyy-MM-ddTHH:mm:ss.fffZ",
                    System.Globalization.CultureInfo.InvariantCulture);

                var newData = new CachedTimestampData(timeUtc, newTimeStampString);
                Interlocked.Exchange(ref _cachedTimestampData, newData);
            }

            return (timeUtc, _cachedTimestampData.CachedFormattedString);
        }

        private DateTime ReserveUniqueTimeUtc()
        {
            var candidateTicks = DateTime.UtcNow.Ticks;

            while (true)
            {
                var previousTicks = Volatile.Read(ref _lastIssuedUtcTicks);
                var nextTicks = candidateTicks <= previousTicks ? previousTicks + 1 : candidateTicks;
                if (Interlocked.CompareExchange(ref _lastIssuedUtcTicks, nextTicks, previousTicks) == previousTicks)
                {
                    return new DateTime(nextTicks, DateTimeKind.Utc);
                }
            }
        }

        public ITagsRegistry TagsRegistry { get; }
        public StackTraceFormatter StackTraceFormatter { get; }
    }
}
