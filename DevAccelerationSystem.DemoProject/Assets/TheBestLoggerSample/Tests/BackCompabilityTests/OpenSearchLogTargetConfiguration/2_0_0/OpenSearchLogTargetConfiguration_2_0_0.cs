using System;
using System.Collections.Generic;
using System.Reflection;

[Serializable]
public class OpenSearchLogTargetConfiguration_2_0_0
{
    public string OpenSearchHostUrl;
    public string OpenSearchSingleLogMethod;
    public string IndexPrefix;
    public string ApiKey;
    public bool Muted;
    public int MinLogLevel;
    public List<OverrideCategory> OverrideCategories;
    public BatchLogsConfiguration BatchLogs;
    public DebugModeConfiguration DebugMode;
    public List<StackTraceConfiguration> StackTraces;
    public bool IsThreadSafe;
    public DispatchingLogsConfiguration DispatchingLogsToMainThread;

    [Serializable]
    public class OverrideCategory
    {
        public string Category;
        public int MinLevel;
    }

    [Serializable]
    public struct BatchLogsConfiguration
    {
        public bool Enabled;
        public int UpdatePeriodMs;
        public int MaxCountLogs;
    }

    [Serializable]
    public class DebugModeConfiguration
    {
        public bool Enabled;
        public int MinLogLevel;
        public List<string> IDs;
        public List<OverrideCategory> OverrideCategories;
    }

    [Serializable]
    public struct StackTraceConfiguration
    {
        public int Level;
        public bool Enabled;
    }

    [Serializable]
    public struct DispatchingLogsConfiguration
    {
        public bool Enabled;
        public bool SingleLogDispatchEnabled;
        public bool BatchLogsDispatchEnabled;
    }
}
