using System;
using UnityEngine.Scripting;

namespace StabilityHub
{
    public class CrashReporterModule : IDisposable
    {
        private CrashReporteriOS _crashReporteriOS;

        public  void RetrieveAndLogPreviousSessionIssues(TheBestLogger.ILogger logger)
        {
#if UNITY_IOS && !UNITY_EDITOR
            _crashReporteriOS?.RetrieveAndLogPreviousSessionIssues(logger);
#endif
        }

        [Preserve]
        public CrashReporterModule(bool iOSModuleEnabled)
        {
#if UNITY_IOS && !UNITY_EDITOR

            if (iOSModuleEnabled)
            {
                _crashReporteriOS = new CrashReporteriOS();
            }
#endif
        }

        public void Dispose()
        {
            _crashReporteriOS?.Dispose();
            _crashReporteriOS = null;
        }
    }
}
