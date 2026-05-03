using UnityEngine;

namespace TheBestLogger
{
    [System.Serializable]
    public class LogTargetCategory
    {
        public string Category;
        [Tooltip("It will override min level of logs for the defined category")]
        public LogLevel MinLevel;

        [Range(0f, 100f)]
        [Tooltip("Percent of logger sessions that should keep this category override active for the current target. The logger rolls this once when the target configuration is applied and keeps the result until the next configuration apply. Only values greater than 0 and less than 100 activate this filter. 0 and 100 do not change category behavior.")]
        public float SessionRolloutPercentage;

        public void ApplyRuntimeDefaults()
        {
            if (SessionRolloutPercentage < 0f)
            {
                SessionRolloutPercentage = 0f;
            }
            else if (SessionRolloutPercentage > 100f)
            {
                SessionRolloutPercentage = 100f;
            }
        }
    }
}
