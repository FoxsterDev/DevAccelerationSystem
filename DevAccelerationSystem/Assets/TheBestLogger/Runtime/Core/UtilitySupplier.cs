using System;
using System.Threading;

namespace TheBestLogger
{
    internal class UtilitySupplier : IUtilitySupplier
    {
        private readonly int _mainThreadId;
        public bool IsMainThread => _mainThreadId == Thread.CurrentThread.ManagedThreadId;
        private readonly long _timezoneOffsetTicks;
        private readonly uint _minTimestampPeriodMs;
        private DateTime LocalTime => new DateTime(DateTime.UtcNow.Ticks + _timezoneOffsetTicks, DateTimeKind.Local);
        private TimestampData _timeStampCachedData;
        
        public UtilitySupplier(uint minTimestampPeriodMs)
        {
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            
            _minTimestampPeriodMs = minTimestampPeriodMs;
            _timeStampCachedData = new TimestampData(DateTime.MinValue, string.Empty);
            //_timezoneOffsetTicks = (DateTime.Now - DateTime.UtcNow).Ticks;
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
    }

}