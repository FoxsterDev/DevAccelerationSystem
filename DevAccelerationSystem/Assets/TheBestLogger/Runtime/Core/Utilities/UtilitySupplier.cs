using System;
using System.Threading;

namespace TheBestLogger.Core.Utilities
{
    internal class UtilitySupplier 
    {
        private readonly int _mainThreadId;
        public bool IsMainThread => _mainThreadId == Thread.CurrentThread.ManagedThreadId;
        private readonly long _timezoneOffsetTicks;
        private readonly uint _minTimestampPeriodMs;
        private DateTime LocalTime => new DateTime(DateTime.UtcNow.Ticks + _timezoneOffsetTicks, DateTimeKind.Local);

        private TimestampData _timeStampCachedData;

        public UtilitySupplier(uint minTimestampPeriodMs, StackTraceFormatter stackTraceFormatter)
        {
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;

            _minTimestampPeriodMs = minTimestampPeriodMs;
            _timeStampCachedData = new TimestampData(DateTime.MinValue, string.Empty);
            TagsRegistry = new ConcurrentTagsRegistry(2, 4);
//_timezoneOffsetTicks = (DateTime.Now - DateTime.UtcNow).Ticks;
            StackTraceFormatter = stackTraceFormatter;
        }

        public (DateTime currentTimeUtc, string timeStampFormatted) GetTimeStamp()
        {
            var timeUtc = DateTime.UtcNow;
            var timeStampCachedData = _timeStampCachedData;

            if ((timeUtc - timeStampCachedData.TimeStampCachedUtc).TotalMilliseconds >= _minTimestampPeriodMs)
            {
                var newTimeStampString = timeUtc.ToString(
                    "yyyy-MM-ddTHH:mm:ss.fffZ",
                    System.Globalization.CultureInfo.InvariantCulture);

                var newData = new TimestampData(timeUtc, newTimeStampString);
                Interlocked.Exchange(ref _timeStampCachedData, newData);
            }

            return (timeUtc, _timeStampCachedData.TimeStampStringCachedFormatted);
        }

        public ITagsRegistry TagsRegistry { get; }
        public StackTraceFormatter StackTraceFormatter { get; }
    }
}
