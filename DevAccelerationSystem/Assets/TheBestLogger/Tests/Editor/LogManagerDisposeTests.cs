using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace TheBestLogger.Tests.Editor
{
    [TestFixture]
    public class LogManagerDisposeTests
    {
        [Test]
        public void Dispose_DoesNotDoubleDisposeWrappedTargets()
        {
            var original = new CountingLogTarget();
            var decorated = new LogTargetBatchLogsDecoration(
                new LogTargetBatchLogsConfiguration
                {
                    Enabled = true,
                    MaxCountLogs = 4,
                    UpdatePeriodMs = 1000
                },
                original,
                DateTime.UtcNow);

            SetStaticField("_wasDisposed", false);
            SetStaticField("_loggers", new ConcurrentDictionary<string, ILogger>());
            SetStaticField("_logSources", Array.Empty<ILogSource>());
            SetStaticField("_targetUpdates", new List<IScheduledUpdate>());
            SetStaticField("_originalLogTargets", new LogTarget[] { original });
            SetStaticField("_decoratedLogTargets", new ILogTarget[] { decorated });

            LogManager.Dispose();

            Assert.That(original.DisposeCallCount, Is.EqualTo(1));

            SetStaticField("_wasDisposed", false);
            SetStaticField("_originalLogTargets", Array.Empty<LogTarget>());
            SetStaticField("_decoratedLogTargets", Array.Empty<ILogTarget>());
        }

        private static void SetStaticField(string fieldName, object value)
        {
            var field = typeof(LogManager).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(field, Is.Not.Null, $"Missing field {fieldName}");
            field.SetValue(null, value);
        }
    }
}
