using System;
using System.Collections.Generic;
using System.Threading;
using TheBestLogger.Core.Utilities;

namespace TheBestLogger.Tests.Editor
{
    internal class MakeSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object state)
        {
            d(state);
        }
    }
    internal class MockUtilitySupplier : IUtilitySupplier
    {
        public bool IsMainThread { get; set; }

        public (DateTime currentTimeUtc, string timeStampFormatted) GetTimeStamp()
        {
            throw new NotImplementedException();
        }

        public ITagsRegistry TagsRegistry { get; }
    }

    internal class MockLogTargetConfiguration : LogTargetConfiguration
    {
    }

    internal class MockLogTarget : LogTarget
    {
        public List<List<(LogLevel level, string category, string message, LogAttributes logAttributes, Exception exception)>> LoggedBatches { get; } = new();

        public override string LogTargetConfigurationName => "FakeLogTargetConfiguration";

        public override void Log(LogLevel level,
                                 string category,
                                 string message,
                                 LogAttributes logAttributes,
                                 Exception exception)
        {
            LogBatch(
                new List<(LogLevel level, string category, string message, LogAttributes logAttributes, Exception exception)>
                    { (level, category, message, logAttributes, exception) });
        }

        public override void LogBatch(
            IReadOnlyList<(LogLevel level, string category, string message, LogAttributes logAttributes, Exception exception)> logBatch)
        {
            LoggedBatches.Add(new List<(LogLevel level, string category, string message, LogAttributes logAttributes, Exception exception)>(logBatch));
        }
    }
}
