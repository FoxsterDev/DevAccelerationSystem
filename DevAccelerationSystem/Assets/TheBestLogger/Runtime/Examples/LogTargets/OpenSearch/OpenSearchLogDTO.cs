namespace TheBestLogger.Examples.LogTargets
{
    [System.Serializable]
    public class OpenSearchLogDTO
    {
        public string GameVersion;
        public string UUID;
        public string DeviceModel;
        public string OS;
        public string Platform;
        public string LogLevel;
        public string Category;
        public string Message;
        //public string Index;
        public string Stacktrace;
        //default key used as @timestamp, but you can reconfigure it in index dashboards of ipensearch
        public string TimeUTC;
        public string Attributes;
        public bool DebugMode;
    }
}