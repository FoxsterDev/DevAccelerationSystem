using System;
using System.Collections.Generic;
using UnityEngine;

namespace Loqui
{
    [CreateAssetMenu(fileName = "LocalizationTextTable", menuName = "Loqui/Text Table")]
    public sealed class LocalizationTextTable : ScriptableObject
    {
        public string Group;
        public List<LocalizationEntry> Entries = new();

        public bool Validate(out string error)
        {
            var seenKeys = new HashSet<string>(StringComparer.Ordinal);
            if (Entries != null)
            {
                foreach (var entry in Entries)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.Key))
                    {
                        error = $"Text table '{name}' has an entry with no key.";
                        return false;
                    }

                    if (!seenKeys.Add(entry.Key))
                    {
                        error = $"Text table '{name}' has a duplicate key '{entry.Key}'.";
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }

        public bool TryGetEntry(string key, out LocalizationEntry entry)
        {
            if (Entries != null && !string.IsNullOrEmpty(key))
            {
                foreach (var candidate in Entries)
                {
                    if (candidate != null && string.Equals(candidate.Key, key, StringComparison.Ordinal))
                    {
                        entry = candidate;
                        return true;
                    }
                }
            }

            entry = null;
            return false;
        }
    }
}
