using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TheBestLogger.Tests.Editor
{
    [TestFixture]
    public sealed class TaskExtensionsTests
    {
        [Test]
        public void FireAndLogWhenExceptions_LogsOriginalException()
        {
            var logger = new RecordingLogger();
            var exception = new InvalidOperationException("boom");
            var task = Task.FromException(exception);

            task.FireAndLogWhenExceptions(logger);

            Assert.That(logger.WaitForError(TimeSpan.FromSeconds(1)), Is.True);
            Assert.That(logger.LastException, Is.SameAs(exception));
        }

        [Test]
        public void RunSafely_WhenExceptionCallbackThrows_DoesNotPropagate()
        {
            var task = Task.FromException(new InvalidOperationException("boom"));
            LogAssert.Expect(LogType.Exception, "ArgumentException: callback failed");
            LogAssert.Expect(LogType.Exception, "InvalidOperationException: boom");

            Assert.DoesNotThrow(() => task.RunSafely(_ => throw new ArgumentException("callback failed"))
                                          .GetAwaiter()
                                          .GetResult());
        }

        [Test]
        public void HandleExceptions_WhenSynchronizationContextMissing_DoesNotThrow()
        {
            var previousContext = SynchronizationContext.Current;
            try
            {
                SynchronizationContext.SetSynchronizationContext(null);
                var task = Task.FromException(new InvalidOperationException("boom"));

                Assert.DoesNotThrow(() => task.HandleExceptions(_ => { }, null));
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previousContext);
            }
        }

        private sealed class RecordingLogger : ILogger
        {
            private readonly ManualResetEventSlim _errorLogged = new(false);

            public Exception LastException { get; private set; }
            public List<string> Messages { get; } = new();

            public bool WaitForError(TimeSpan timeout)
            {
                return _errorLogged.Wait(timeout);
            }

            public void Dispose()
            {
            }

            public void LogException(Exception ex, LogAttributes logAttributes = null)
            {
                LastException = ex;
                _errorLogged.Set();
            }

            public void LogError(string message, LogAttributes logAttributes = null)
            {
                Messages.Add(message);
            }

            public void LogError(string message, Exception exception, LogAttributes logAttributes = null)
            {
                Messages.Add(message);
                LastException = exception;
                _errorLogged.Set();
            }

            public void LogWarning(string message, LogAttributes logAttributes = null)
            {
                Messages.Add(message);
            }

            public void LogInfo(string message, LogAttributes logAttributes = null)
            {
                Messages.Add(message);
            }

            public void LogDebug(string message, LogAttributes logAttributes = null)
            {
                Messages.Add(message);
            }

            public void LogFormat(LogLevel logLevel, string message)
            {
                Messages.Add(message);
            }

            public void LogFormat(LogLevel logLevel,
                                  string message,
                                  LogAttributes logAttributes = null,
                                  params object[] args)
            {
                Messages.Add(message);
            }

            public void LogFormat<T1>(LogLevel level, string message, LogAttributes attrs, in T1 arg1)
            {
                Messages.Add(message);
            }

            public void LogFormat<T1, T2>(LogLevel level, string message, LogAttributes attrs, in T1 arg1, in T2 arg2)
            {
                Messages.Add(message);
            }

            public void LogFormat<T1, T2, T3>(LogLevel level, string message, LogAttributes attrs, in T1 arg1, in T2 arg2, in T3 arg3)
            {
                Messages.Add(message);
            }

            public void LogFormat<T1>(LogLevel level, string message, in T1 arg1)
            {
                Messages.Add(message);
            }

            public void LogFormat<T1, T2>(LogLevel level, string message, in T1 arg1, in T2 arg2)
            {
                Messages.Add(message);
            }

            public void LogFormat<T1, T2, T3>(LogLevel level, string message, in T1 arg1, in T2 arg2, in T3 arg3)
            {
                Messages.Add(message);
            }
        }
    }
}
