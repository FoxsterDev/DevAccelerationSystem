using System;

namespace TheBestLogger.Core.Utilities
{
    public interface IUtilitySupplier
    {
        bool IsMainThread { get; }
        (DateTime currentTimeUtc, string timeStampFormatted) GetTimeStamp();
        ITagsRegistry TagsRegistry{ get; }
    }
}