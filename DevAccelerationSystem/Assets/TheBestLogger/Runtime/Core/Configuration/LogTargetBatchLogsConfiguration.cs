using UnityEngine.Serialization;

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
    
    [System.Serializable]
    public struct LogTargetDispatchingLogsToMainThreadConfiguration
    {
        public bool Enabled;
        [FormerlySerializedAs("SingleLogDispatch")]
        public bool SingleLogDispatchEnabled;
        [FormerlySerializedAs("BatchLogsDispatch")]
        public bool BatchLogsDispatchEnabled;
    }
}