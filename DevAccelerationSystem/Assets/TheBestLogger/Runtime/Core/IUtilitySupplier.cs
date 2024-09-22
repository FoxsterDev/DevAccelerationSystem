using System;

namespace TheBestLogger
{
    internal interface IUtilitySupplier
    {
        bool IsMainThread { get; }
        (DateTime currentTimeUtc, string timeStampFormatted) GetTimeStamp();
    }
}