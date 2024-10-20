using System;
using System.Collections.Concurrent;
using System.Linq;
using UnityEngine.Scripting;

namespace TheBestLogger.Core.Utilities
{
    public class ConcurrentTagsRegistry : ITagsRegistry
    {
        // A concurrent dictionary to store tags
        private readonly ConcurrentDictionary<string, bool> _tags;

        // A cached list of tags
        private string[] _cachedTags = Array.Empty<string>();
        private volatile bool _isCacheDirty = true;
        private readonly object _cacheLock = new object(); // Lock for cache updates

        [Preserve]
        public ConcurrentTagsRegistry(int concurrencyLevel, int capacity)
        {
            _tags = new ConcurrentDictionary<string, bool>(concurrencyLevel, capacity);
        }

        // Add a tag (updates the cache)
        public bool AddTag(string tag)
        {
            if (_tags.TryAdd(tag, true))
            {
                _isCacheDirty = true; // Volatile ensures changes are visible across threads
                return true;
            }

            return false;
        }

        // Remove a tag (updates the cache)
        public bool RemoveTag(string tag)
        {
            if (_tags.TryRemove(tag, out _))
            {
                _isCacheDirty = true;
                return true;
            }

            return false;
        }

        // Check if a tag exists
        public bool ContainsTag(string tag)
        {
            return _tags.ContainsKey(tag);
        }

        // Get all tags (returns cached result if available)
        public string[] GetAllTags()
        {
            if (_isCacheDirty)
            {
                lock (_cacheLock)
                {
                    if (_isCacheDirty)  // Double-check inside the lock
                    {
                        _cachedTags = _tags.Keys.ToArray();
                        _isCacheDirty = false;
                    }
                }
            }

            return _cachedTags;
        }
    }
}
