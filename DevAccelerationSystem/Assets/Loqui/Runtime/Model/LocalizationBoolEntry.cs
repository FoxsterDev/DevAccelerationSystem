using System;
using UnityEngine;

namespace Loqui
{
    [Serializable]
    public class LocalizationBoolEntry
    {
        [Tooltip("Unique config key, e.g. 'mission.show_progress_bar'. Unique across the whole catalog.")]
        public string Key;

        [Tooltip("Default value plus optional per-platform iOS / Android overrides.")]
        public LocalizationBoolValues Values = new();

        [Tooltip("Author-facing: where and how this flag is used. Not shipped at runtime.")]
        [TextArea]
        public string Context;
    }
}
