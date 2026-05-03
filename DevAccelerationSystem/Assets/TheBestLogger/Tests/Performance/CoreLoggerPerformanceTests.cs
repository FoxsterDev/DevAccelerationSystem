using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using NUnit.Framework;
using TheBestLogger.Core.Utilities;
using TheBestLogger.Examples.LogTargets;
using Unity.PerformanceTesting;
using UnityEngine;

namespace TheBestLogger.Tests.Performance
{
    [TestFixture]
    public class CoreLoggerPerformanceTests
    {
        private const int HotPathIterations = 500;
        private const int QueuedLogCount = 1000;
        private const int OpenSearchBatchSize = 20;
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);
        private static readonly DateTime OpenSearchFormatterPerfUtc =
            DateTime.Parse("2026-04-29T19:15:42.0267890Z", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

        [Test, Performance]
        public void LogInfo_HotPath_RecordsTimeAndGcMetrics()
        {
            var target = new PerformanceCountingLogTarget();
            var logger = CreateLogger(target);

            Measure.Method(() => logger.LogInfo("constant-message"))
                   .SampleGroup(new SampleGroup("CoreLogger.LogInfo", SampleUnit.Millisecond))
                   .WarmupCount(3)
                   .MeasurementCount(12)
                   .IterationsPerMeasurement(HotPathIterations)
                   .GC()
                   .Run();

            Assert.That(target.LoggedCount, Is.GreaterThan(0));
        }

        [Test, Performance]
        public void LogFormat_HotPath_RecordsTimeAndGcMetrics()
        {
            var target = new PerformanceCountingLogTarget();
            var logger = CreateLogger(target);
            var counter = 0;

            Measure.Method(() =>
                   {
                       logger.LogFormat(LogLevel.Info, "message-{0}", counter++);
                   })
                   .SampleGroup(new SampleGroup("CoreLogger.LogFormat", SampleUnit.Millisecond))
                   .WarmupCount(3)
                   .MeasurementCount(12)
                   .IterationsPerMeasurement(HotPathIterations)
                   .GC()
                   .Run();

            Assert.That(target.LoggedCount, Is.GreaterThan(0));
        }

        [Test, Performance]
        public void DispatchingFlush_QueuedBurst_RecordsTimeAndGcMetrics()
        {
            PerformanceCountingLogTarget target = null;
            PerformanceQueuedSynchronizationContext syncContext = null;
            UtilitySupplier utilitySupplier = null;
            ILogTarget dispatchingTarget = null;

            Measure.Method(() => syncContext.FlushPostedCallbacks())
                   .SampleGroup(new SampleGroup("Dispatch.FlushQueuedBurst", SampleUnit.Millisecond))
                   .SetUp(() =>
                   {
                       target = new PerformanceCountingLogTarget(isThreadSafe: false, dispatchToMainThread: true);
                       syncContext = new PerformanceQueuedSynchronizationContext();
                       utilitySupplier = CreateUtilitySupplier();
                       dispatchingTarget = new LogTargetDispatchingLogsToMainThreadDecoration(
                           new LogTargetDispatchingLogsToMainThreadConfiguration
                           {
                               Enabled = true,
                               SingleLogDispatchEnabled = true,
                               BatchLogsDispatchEnabled = true
                           },
                           target,
                           syncContext,
                           utilitySupplier);

                       RunOnWorkerThread(() =>
                       {
                           for (var iteration = 0; iteration < QueuedLogCount; iteration++)
                           {
                               dispatchingTarget.Log(LogLevel.Info,
                                                     "PerfCategory",
                                                     "queued-message",
                                                     new LogAttributes(LogImportance.Important) { TimeUtc = DateTime.UtcNow },
                                                     null);
                           }
                       });
                   })
                   .CleanUp(() =>
                   {
                       Assert.That(target.LoggedCount, Is.EqualTo(QueuedLogCount));
                   })
                   .WarmupCount(2)
                   .MeasurementCount(10)
                   .GC()
                   .Run();
        }

        [Test, Performance]
        public void BatchFlush_Burst_RecordsTimeAndGcMetrics()
        {
            const int batchSize = 64;
            const int updatePeriodMs = 1000;
            PerformanceCountingLogTarget target = null;
            LogTargetBatchLogsDecoration batchingTarget = null;
            DateTime startTimeUtc = default;

            Measure.Method(() =>
                   {
                       var currentTimeUtc = startTimeUtc;
                       while (target.LoggedCount < QueuedLogCount)
                       {
                           currentTimeUtc = currentTimeUtc.AddMilliseconds(updatePeriodMs + 1);
                           ((IScheduledUpdate) batchingTarget).Update(currentTimeUtc, updatePeriodMs + 1);
                       }
                   })
                   .SampleGroup(new SampleGroup("Batch.FlushBurst", SampleUnit.Millisecond))
                   .SetUp(() =>
                   {
                       target = new PerformanceCountingLogTarget();
                       startTimeUtc = DateTime.UtcNow;
                       batchingTarget = new LogTargetBatchLogsDecoration(
                           new LogTargetBatchLogsConfiguration
                           {
                               Enabled = true,
                               MaxCountLogs = batchSize,
                               UpdatePeriodMs = updatePeriodMs
                           },
                           target,
                           startTimeUtc);

                       for (var iteration = 0; iteration < QueuedLogCount; iteration++)
                       {
                           ((ILogTarget) batchingTarget).Log(LogLevel.Info,
                                                             "PerfCategory",
                                                             "batched-message",
                                                             new LogAttributes(LogImportance.Important)
                                                             {
                                                                 TimeUtc = startTimeUtc.AddMilliseconds(iteration)
                                                             },
                                                             null);
                       }
                   })
                   .CleanUp(() =>
                   {
                       Assert.That(target.LoggedCount, Is.EqualTo(QueuedLogCount));
                   })
                   .WarmupCount(2)
                   .MeasurementCount(10)
                   .GC()
                   .Run();
        }

        [Test, Performance]
        public void OpenSearchDto_ManualJson_RecordsTimeAndGcMetrics()
        {
            PrepareDerivedOpenSearchContext();

            Measure.Method(() =>
                   {
                       var dto = CreateDerivedDto(7);
                       dto.PrepareForJsonSerialization();
                       var sb = new OpenSearchPayloadBuilder(notNested: true);
                       try
                       {
                           dto.WriteJson(ref sb);
                       }
                       finally
                       {
                           sb.Dispose();
                       }
                   })
                   .SampleGroup(new SampleGroup("OpenSearchDTO.ManualJson", SampleUnit.Millisecond))
                   .WarmupCount(3)
                   .MeasurementCount(12)
                   .IterationsPerMeasurement(200)
                   .GC()
                   .Run();
        }

        [Test, Performance]
        public void OpenSearchDto_LegacyJsonUtility_RecordsTimeAndGcMetrics()
        {
            PrepareDerivedOpenSearchContext();

            Measure.Method(() =>
                   {
                       var dto = CreateDerivedDto(7);
                       JsonUtility.ToJson(dto);
                   })
                   .SampleGroup(new SampleGroup("OpenSearchDTO.LegacyJsonUtility", SampleUnit.Millisecond))
                   .WarmupCount(3)
                   .MeasurementCount(12)
                   .IterationsPerMeasurement(200)
                   .GC()
                   .Run();
        }

        [Test, Performance]
        public void OpenSearchBatch_ManualJson_RecordsTimeAndGcMetrics()
        {
            var batch = CreateOpenSearchBatch(OpenSearchBatchSize);
            MockOpenSearchLogTarget target = null;

            Measure.Method(() => target.LogBatch(batch))
                   .SampleGroup(new SampleGroup("OpenSearchBatch.ManualJson", SampleUnit.Millisecond))
                   .SetUp(() =>
                   {
                       PrepareDerivedOpenSearchContext();
                       MockOpenSearchLogTarget.ClearCapturedPayloads();
                       target = new MockOpenSearchLogTarget(() => new GameSessionOpenSearchLogDTOExample());
                       target.ApplyConfiguration(CreateOpenSearchConfiguration());
                   })
                   .WarmupCount(2)
                   .MeasurementCount(10)
                   .GC()
                   .Run();
        }

        [Test, Performance]
        public void OpenSearchBatch_LegacyJsonUtility_RecordsTimeAndGcMetrics()
        {
            var batch = CreateOpenSearchBatch(OpenSearchBatchSize);

            Measure.Method(() => BuildLegacyOpenSearchBatchPayload(batch))
                   .SampleGroup(new SampleGroup("OpenSearchBatch.LegacyJsonUtility", SampleUnit.Millisecond))
                   .SetUp(PrepareDerivedOpenSearchContext)
                   .WarmupCount(2)
                   .MeasurementCount(10)
                   .GC()
                   .Run();
        }

        [Test, Performance]
        public void OpenSearchTimestamp_ToStringFff_RecordsTimeAndGcMetrics()
        {
            Measure.Method(() =>
                   {
                       _ = OpenSearchFormatterPerfUtc.ToString(OpenSearchTimestampFormatter.Format,
                                                               CultureInfo.InvariantCulture);
                   })
                   .SampleGroup(new SampleGroup("OpenSearchTimestamp.ToStringFff", SampleUnit.Millisecond))
                   .WarmupCount(3)
                   .MeasurementCount(12)
                   .IterationsPerMeasurement(2000)
                   .GC()
                   .Run();
        }

        [Test, Performance]
        public void OpenSearchTimestamp_StringCreateTryFormat_RecordsTimeAndGcMetrics()
        {
            Measure.Method(() =>
                   {
                       _ = OpenSearchTimestampFormatter.FormatUtc(OpenSearchFormatterPerfUtc);
                   })
                   .SampleGroup(new SampleGroup("OpenSearchTimestamp.StringCreateTryFormat", SampleUnit.Millisecond))
                   .WarmupCount(3)
                   .MeasurementCount(12)
                   .IterationsPerMeasurement(2000)
                   .GC()
                   .Run();
        }

        private static CoreLogger CreateLogger(params ILogTarget[] logTargets)
        {
            return new CoreLogger("PerfCategory",
                                  string.Empty,
                                  logTargets,
                                  CreateUtilitySupplier(),
                                  512);
        }

        private static UtilitySupplier CreateUtilitySupplier()
        {
            return new UtilitySupplier(0,
                                       new StackTraceFormatter(Application.dataPath, new StackTraceFormatterConfiguration()));
        }

        private static void RunOnWorkerThread(Action action)
        {
            Exception exception = null;
            using var completed = new ManualResetEventSlim(false);
            var worker = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                finally
                {
                    completed.Set();
                }
            });

            worker.Start();
            Assert.That(completed.Wait(TimeSpan.FromSeconds(5)), Is.True, "Worker thread timed out.");
            if (exception != null)
            {
                throw exception;
            }
        }

        private static void PrepareDerivedOpenSearchContext()
        {
            GameSessionOpenSearchLogDTOExample.SetAppId("perf-app", "perf-uuid");
            GameSessionOpenSearchLogDTOExample.SetSharedAppId("shared-perf");
            GameSessionOpenSearchLogDTOExample.SetPlayerIdAndSession("player-perf", "session-perf");
        }

        private static GameSessionOpenSearchLogDTOExample CreateDerivedDto(int index)
        {
            return new GameSessionOpenSearchLogDTOExample
            {
                GameVersion = Application.version,
                UUID = SystemInfo.deviceUniqueIdentifier,
                DeviceModel = SystemInfo.deviceModel,
                OS = SystemInfo.operatingSystem,
                Platform = Application.platform.ToString(),
                LogLevel = "Warn",
                Category = "Gameplay",
                Message = $"message-{index}",
                Stacktrace = "stack-line\nnext-line",
                TimeUTC = "2026-04-29T19:15:42.026Z",
                Attributes = "{\"session\":\"session-perf\"}",
                DebugMode = true,
                Tags = new[] { "perf", "manual-json" }
            };
        }

        private static List<LogEntry> CreateOpenSearchBatch(int count)
        {
            var batch = new List<LogEntry>(count);
            var baseTimeUtc = DateTime.Parse("2026-04-29T19:15:42.0260000Z",
                                             CultureInfo.InvariantCulture,
                                             DateTimeStyles.RoundtripKind);
            for (var index = 0; index < count; index++)
            {
                var timeUtc = baseTimeUtc.AddTicks(index);
                batch.Add(new LogEntry(LogLevel.Warning,
                                       "Gameplay",
                                       $"message-{index}",
                                       new LogAttributes("session", $"session-{index}")
                                       {
                                           StackTrace = $"stack-line-{index}\nnext-line",
                                           TimeStampFormatted = OpenSearchTimestampFormatter.FormatUtc(timeUtc),
                                           TimeUtc = timeUtc,
                                           Tags = new[] { "perf", $"tag-{index % 4}" }
                                       },
                                       null));
            }

            return batch;
        }

        private static OpenSearchLogTargetConfiguration CreateOpenSearchConfiguration()
        {
            return new OpenSearchLogTargetConfiguration
            {
                OpenSearchHostUrl = "mock://sample-opensearch",
                OpenSearchSingleLogMethod = "/logs",
                IndexPrefix = "thebestlogger-perf-",
                ApiKey = "perf-key",
                MinLogLevel = LogLevel.Debug,
                DebugMode = new DebugModeConfiguration(),
                BatchLogs = new LogTargetBatchLogsConfiguration(),
                DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration()
            };
        }

        private static byte[] BuildLegacyOpenSearchBatchPayload(IReadOnlyList<LogEntry> batch)
        {
            var builder = new StringBuilder(batch.Count * 256);
            var indexName = "thebestlogger-perf-" + DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            for (var index = 0; index < batch.Count; index++)
            {
                var entry = batch[index];
                var dto = CreateDerivedDto(index);
                dto.Category = entry.Category;
                dto.Message = entry.Message;
                dto.Stacktrace = entry.Attributes.StackTrace;
                dto.TimeUTC = entry.Attributes.TimeStampFormatted;
                dto.Attributes = entry.Attributes.Props.ToSimpleNotEscapedJson();
                dto.Tags = entry.Attributes.Tags;

                builder.Append("{ \"index\" : { \"_index\" : \"");
                builder.Append(indexName);
                builder.AppendLine("\" } }");
                builder.AppendLine(JsonUtility.ToJson(dto));
            }

            return Utf8NoBom.GetBytes(builder.ToString());
        }

        private sealed class PerformanceQueuedSynchronizationContext : SynchronizationContext
        {
            private readonly ConcurrentQueue<(SendOrPostCallback callback, object state)> _queue = new();

            public override void Post(SendOrPostCallback d, object state)
            {
                _queue.Enqueue((d, state));
            }

            public void FlushPostedCallbacks()
            {
                while (_queue.TryDequeue(out var item))
                {
                    item.callback(item.state);
                }
            }
        }

        private sealed class PerformanceCountingLogTarget : LogTarget
        {
            private int _loggedCount;
            private int _batchCount;

            public int LoggedCount => _loggedCount;
            public int BatchCount => _batchCount;

            public PerformanceCountingLogTarget(bool isThreadSafe = true, bool dispatchToMainThread = false)
            {
                ApplyConfiguration(new PerformanceLogTargetConfiguration
                {
                    MinLogLevel = LogLevel.Debug,
                    IsThreadSafe = isThreadSafe,
                    DebugMode = new DebugModeConfiguration(),
                    BatchLogs = new LogTargetBatchLogsConfiguration(),
                    DispatchingLogsToMainThread = new LogTargetDispatchingLogsToMainThreadConfiguration
                    {
                        Enabled = dispatchToMainThread,
                        SingleLogDispatchEnabled = dispatchToMainThread,
                        BatchLogsDispatchEnabled = dispatchToMainThread
                    }
                });
            }

            public override string LogTargetConfigurationName => nameof(PerformanceLogTargetConfiguration);

            public override void Log(LogLevel level,
                                     string category,
                                     string message,
                                     LogAttributes logAttributes,
                                     Exception exception)
            {
                Interlocked.Increment(ref _loggedCount);
            }

            public override void LogBatch(IReadOnlyList<LogEntry> logBatch)
            {
                if (logBatch == null)
                {
                    return;
                }

                Interlocked.Increment(ref _batchCount);
                Interlocked.Add(ref _loggedCount, logBatch.Count);
            }
        }

        private sealed class PerformanceLogTargetConfiguration : LogTargetConfiguration
        {
        }
    }
}
