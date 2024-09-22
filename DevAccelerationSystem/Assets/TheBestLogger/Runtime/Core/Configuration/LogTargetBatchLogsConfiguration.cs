namespace TheBestLogger
{
    [System.Serializable]
    public struct LogTargetBatchLogsConfiguration
    {
        public bool Enabled;
        /// <summary>
        /// In which period it will send bucket logs(at least 1 log) to original logtarget 
        /// </summary>
        public uint UpdatePeriodMs;
        
        public uint MaxCountLogs;
    }
}