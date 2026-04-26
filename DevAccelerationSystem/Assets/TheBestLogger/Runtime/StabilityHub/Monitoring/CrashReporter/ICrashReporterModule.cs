using System;
using TheBestLogger;

namespace StabilityHub
{
    internal interface ICrashReporterModule : IDisposable
    {
        void RetrieveAndLogPreviousSessionIssues(ILogger logger);
    }
}
