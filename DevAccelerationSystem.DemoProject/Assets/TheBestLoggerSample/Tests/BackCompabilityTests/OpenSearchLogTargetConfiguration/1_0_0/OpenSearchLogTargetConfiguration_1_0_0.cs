using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;  // For Unity compatibility (optional)

namespace TheBestLogger.Integration.Tests
{
// Add [System.Serializable] for Unity's JsonUtility compatibility
    [System.Serializable]
    public class OpenSearchLogTargetConfiguration_1_0_0
    {
        // Root fields
        public bool Muted;
        public int MinLogLevel;
        public List<OverrideCategory> OverrideCategories;
        public BatchLogsConfig BatchLogs;
        public DebugModeConfig DebugMode;
        public bool IsThreadSafe;
        public bool ShowTimestamp;
        public string OpenSearchHostUrl;
        public string OpenSearchSingleLogMethod;
        public string IndexPrefix;
        public string ApiKey;

        // Nested class for OverrideCategories
        [System.Serializable]
        public class OverrideCategory
        {
            public string Category;
            public int MinLevel;
        }

        // Nested class for BatchLogs configuration
        [System.Serializable]
        public class BatchLogsConfig
        {
            public bool Enabled;
            public int UpdatePeriodMs;
            public int MaxCountLogs;
        }

        // Nested class for DebugMode configuration
        [System.Serializable]
        public class DebugModeConfig
        {
            public bool Enabled;
            public int MinLogLevel;
            public List<string> IDs;
            public List<OverrideCategory> OverrideCategories;
        }

        // Method to deserialize JSON string into an instance of LoggerConfiguration using Newtonsoft.Json
        public static OpenSearchLogTargetConfiguration_1_0_0 FromJson(string jsonString)
        {
            return JsonConvert.DeserializeObject<OpenSearchLogTargetConfiguration_1_0_0>(jsonString);
        }

        // Method to deserialize JSON string using Unity's JsonUtility (for Unity compatibility)
        public static OpenSearchLogTargetConfiguration_1_0_0 FromJsonUnity(string jsonString)
        {
            return JsonUtility.FromJson<OpenSearchLogTargetConfiguration_1_0_0>(jsonString);
        }
    }
}
