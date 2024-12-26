using System;
using System.Collections.Generic;
using System.Threading;
using TheBestLogger.Core.Utilities;
using UnityEngine;

namespace TheBestLogger.Tests.Editor
{
    internal class MakeSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object state)
        {
            d(state);
        }
    }
    internal class MockUtilitySupplier : UtilitySupplier
    {
        public new bool IsMainThread { get; set; }

        public new ITagsRegistry TagsRegistry { get; }

        public MockUtilitySupplier(uint minTimestampPeriodMs, StackTraceFormatter stackTraceFormatter)
            : base(minTimestampPeriodMs, stackTraceFormatter)
        {
        }

        public MockUtilitySupplier()
            : base(10, new StackTraceFormatter(Application.dataPath, new StackTraceFormatterConfiguration()))
        {
            
        }
    }

    internal class MockLogTargetConfiguration : LogTargetConfiguration
    {
    }

    internal class MockLogTarget : LogTarget
    {
        public List<List<(LogLevel level, string category, string message, LogAttributes logAttributes, Exception exception)>> LoggedBatches { get; } = new();

        public void SetDebugMode(bool mode)
        {
            ((ILogTarget)(this)).DebugModeEnabled = mode;
        }

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
