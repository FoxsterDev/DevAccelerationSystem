using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

namespace TheBestLogger
{
    public abstract class SafeThirdPartyLogTarget : LogTarget
    {
        private static bool? _successfullyInit;

        [Preserve]
        public SafeThirdPartyLogTarget()
        {
        }

        [HideInCallstack]
        public override void Log(LogLevel level,
                                 string category,
                                 string message,
                                 LogAttributes logAttributes,
                                 Exception exception = null)
        {
            if (_successfullyInit == false) return;
            try
            {
                if (!IsThirdPartyReady) return;

                if (!_successfullyInit.HasValue)
                {
                    _successfullyInit = false;
                    TryCreateThirdPartyLogMethodDelegate();
                    _successfullyInit = true;
                }

                if (_successfullyInit == true)
                {
                   CallThirdPartyLogMethod(level, category, message, logAttributes, exception);
                }
            }
            catch (Exception ex)
            {
                _successfullyInit = false;
                Mute(true);
                UnityEngine.Debug.LogException(ex);
            }
        }

        //  EXAMPLE FOR BACKTRACE
        // _handleUnityMessageDelegate.Invoke(BacktraceClient.Instance, message, stackTrace, type);
        public abstract void CallThirdPartyLogMethod(LogLevel level,
                                                   string category,
                                                   string message,
                                                   LogAttributes logAttributes,
                                                   Exception exception = null);

        public override void LogBatch(
            IReadOnlyList<(LogLevel level, string category, string message, LogAttributes logAttributes, Exception
                exception)> logBatch)
        {
            foreach (var logEntry in logBatch)
            {
                Log(logEntry.level, logEntry.category, logEntry.message, logEntry.logAttributes, logEntry.exception);
            }
        }

        protected abstract bool IsThirdPartyReady { get; }

        /* EXAMPLE FOR BACKTRACE
         Action<BacktraceClient, string, string, LogType> _handleUnityMessageDelegate
         *   var _unityLogCallback = typeof(BacktraceClient).GetMethod(
                    "HandleUnityMessage",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (_unityLogCallback == null)
                    throw new ArgumentException("HandleUnityMessage method not found on BacktraceClient.");

                _handleUnityMessageDelegate = (Action<BacktraceClient, string, string, LogType>)Delegate.CreateDelegate(
                    typeof(Action<BacktraceClient, string, string, LogType>), 
                    _unityLogCallback
                );
         */
        protected abstract void TryCreateThirdPartyLogMethodDelegate();

        public override void Dispose()
        {
            base.Dispose();
            _successfullyInit = false;
        }
    }
}
