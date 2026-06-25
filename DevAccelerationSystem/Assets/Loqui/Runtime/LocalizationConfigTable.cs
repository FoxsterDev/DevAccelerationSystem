using System;
using System.Collections.Generic;
using UnityEngine;

namespace Loqui
{
    [Serializable]
    public class LocalizationBoolEntry
    {
        public string Key;
        public LocalizationBoolValues Values = new();
        [TextArea] public string Notes;
    }

    [CreateAssetMenu(fileName = "LocalizationConfigTable", menuName = "Loqui/Config Table")]
    public sealed class LocalizationConfigTable : ScriptableObject
    {
        public List<LocalizationBoolEntry> Bools = new();

        public bool Validate(out string error)
        {
            if (Bools != null)
            {
                foreach (var entry in Bools)
                {
                    if (entry != null && string.IsNullOrEmpty(entry.Key))
                    {
                        error = $"Config table '{name}' has a bool entry with an empty key.";
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }
    }
}
