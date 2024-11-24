namespace StabilityHub.Monitoring
{
    [System.Serializable]
    internal struct CrashReporterModuleConfiguration
    {
        public bool Enabled;
        public CrashReporteriOSConfiguration IOS;
    }
}
