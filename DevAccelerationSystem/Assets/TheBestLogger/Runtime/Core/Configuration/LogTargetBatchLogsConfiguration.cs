namespace TheBestLogger
{
    [System.Serializable]
    public sealed class LogTargetBatchLogsConfiguration
    {
        internal const uint MIN_UPDATE_PERIOD_MS = 100;
        internal const uint MIN_BATCH_SIZE = 1;
        internal const uint MAX_BATCH_SIZE = 4096;

        public bool Enabled;
        /// <summary>
        /// In which period it will send bucket logs(at least 1 log) to original logtarget 
        /// </summary>
        public uint UpdatePeriodMs;
        
        public uint MaxCountLogs;

        internal void ApplyRuntimeDefaults()
        {
            if (UpdatePeriodMs < MIN_UPDATE_PERIOD_MS)
            {
                UpdatePeriodMs = MIN_UPDATE_PERIOD_MS;
            }

            if (MaxCountLogs < MIN_BATCH_SIZE)
            {
                MaxCountLogs = MIN_BATCH_SIZE;
            }
            else if (MaxCountLogs > MAX_BATCH_SIZE)
            {
                MaxCountLogs = MAX_BATCH_SIZE;
            }
        }
    }
    
    [System.Serializable]
    public sealed class LogTargetDispatchingLogsToMainThreadConfiguration
    {
        public bool Enabled;
        public bool SingleLogDispatchEnabled;
        public bool BatchLogsDispatchEnabled;
    }
}
