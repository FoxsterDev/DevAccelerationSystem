using System;

namespace TheBestLogger
{
    public class TimestampData
    {
        public readonly DateTime TimeStampCachedUtc;
        public readonly string TimeStampStringCachedFormatted;

        public TimestampData(DateTime timeStampCachedUtc, string timeStampStringCachedFormatted)
        {
            TimeStampCachedUtc = timeStampCachedUtc;
            TimeStampStringCachedFormatted = timeStampStringCachedFormatted;
        }
    }
}