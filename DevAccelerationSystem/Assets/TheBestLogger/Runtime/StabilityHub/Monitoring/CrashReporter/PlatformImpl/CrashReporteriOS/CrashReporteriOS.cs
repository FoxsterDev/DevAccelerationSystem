using System;
using System.Collections.Generic;
using TheBestLogger;
using UnityEngine;
using UnityEngine.Scripting;

namespace StabilityHub
{
    public class CrashReporteriOS : IDisposable
    {
        [Preserve]
        public CrashReporteriOS()
        {

        }

        public  void RetrieveAndLogPreviousSessionIssues(TheBestLogger.ILogger logger)
        {
            System.Threading.Tasks.Task.Run(
                () =>
                {
                    //thread safe
                    var reports = CrashReport.reports;
                    if (reports.Length > 0)
                    {
                        var uniqueTimes = new HashSet<DateTime>(reports.Length);

                        foreach (var report in reports)
                        {
                            if (uniqueTimes.Add(report.time))
                            {
                                var text = report.text;
                                if (!string.IsNullOrEmpty(text))
                                {
                                    text = text.Length > 256
                                               ? text.Substring(0, 256)
                                               : text;
                                }

                                logger.LogError("Crash was detected!", new LogAttributes(LogImportance.Critical).
                                                                       Add("Time", report.time.ToLongDateString()).Add("Text", text));

                            }
                        }
                        CrashReport.RemoveAll();
                    }
                    else
                    {
                        logger.LogDebug("No crashes detected!");
                    }
                }).FireAndLogWhenExceptions(logger);
        }

        public void Dispose()
        {

        }
    }
}
