using UnityEngine;

namespace TheBestLogger
{
    [System.Serializable]
    public class LogTargetCategory
    {
        public string Category;
        [Tooltip("It will override min level of logs for the defined category")]
        public LogLevel MinLevel;
    }
}
