using System;
using UnityEngine;

namespace TheBestLogger
{
    [System.Serializable]
    public class DebugModeConfiguration
    {
        [Header("ACTIVATION")]
        [Tooltip("Master switch for this target's debug override. When enabled, this target can participate in its configured session rollout and can also be activated by an explicit debugId allowlist match. When disabled, neither path can activate DebugMode for this target.")]
        public bool Enabled;

        [Header("DEBUG OVERRIDE OUTPUT")]
        [Tooltip("Min log level that becomes active after DebugMode is enabled for this target. Keep the target's top-level MinLogLevel strict for normal production behavior and use this override for temporary verbose diagnostics.")]
        public LogLevel MinLogLevel = LogLevel.Warning;

        [Header("SESSION DEBUG ROLLOUT")]
        [Range(0f, 100f)]
        [Tooltip("Percent of logger sessions that should randomly enable DebugMode for this target. The logger rolls this once per target on LogManager.Initialize(...) and keeps the result for the whole current logger session. Supports fractional values such as 2.5.")]
        public float SessionDebugRolloutPercentage;

        [Header("EXPLICIT ALLOWLIST")]
        [Tooltip("Explicit debugId allowlist for this target. DebugMode also becomes active when LogManager.Initialize(..., debugId) or LogManager.SetDebugMode(debugId, true) is called with a value from this list. This path is independent from session rollout.")]
        public string[] IDs;

        [Header("DEBUG CATEGORY OVERRIDES")]
        [Tooltip("Optional per-category min level overrides that apply only while DebugMode is active for this target. Use this to keep most categories quiet while opening only the areas you need for investigation.")]
        public LogTargetCategory[] OverrideCategories;

        public void ApplyRuntimeDefaults()
        {
            if (SessionDebugRolloutPercentage < 0f)
            {
                SessionDebugRolloutPercentage = 0f;
            }
            else if (SessionDebugRolloutPercentage > 100f)
            {
                SessionDebugRolloutPercentage = 100f;
            }

            IDs ??= Array.Empty<string>();
            OverrideCategories ??= Array.Empty<LogTargetCategory>();
            for (var index = 0; index < OverrideCategories.Length; index++)
            {
                OverrideCategories[index]?.ApplyRuntimeDefaults();
            }
        }
    }
}
