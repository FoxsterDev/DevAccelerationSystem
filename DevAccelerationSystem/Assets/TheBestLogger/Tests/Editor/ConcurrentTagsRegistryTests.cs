using System.Linq;
using System.Threading.Tasks;
using TheBestLogger.Core.Utilities;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using TheBestLogger;

namespace TheBestLogger.Tests.Editor
{
    [TestFixture]
    public class ConcurrentTagsRegistryTests
    {
        private ConcurrentTagsRegistry _registry;

        [SetUp]
        public void SetUp()
        {
            _registry = new ConcurrentTagsRegistry(4, 10);
        }

        [Test]
        public void AddTag_TagDoesNotExist_ReturnsTrueAndAddsTag()
        {
            // Arrange
            var tag = "TestTag";

            // Act
            var result = _registry.AddTag(tag);

            // Assert
            Assert.IsTrue(result);
            Assert.IsTrue(_registry.ContainsTag(tag));
        }

        [Test]
        public void AddTag_TagAlreadyExists_ReturnsFalse()
        {
            // Arrange
            var tag = "TestTag";
            _registry.AddTag(tag);

            // Act
            var result = _registry.AddTag(tag);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void RemoveTag_TagExists_ReturnsTrueAndRemovesTag()
        {
            // Arrange
            var tag = "TestTag";
            _registry.AddTag(tag);

            // Act
            var result = _registry.RemoveTag(tag);

            // Assert
            Assert.IsTrue(result);
            Assert.IsFalse(_registry.ContainsTag(tag));
        }

        [Test]
        public void RemoveTag_TagDoesNotExist_ReturnsFalse()
        {
            // Arrange
            var tag = "NonExistentTag";

            // Act
            var result = _registry.RemoveTag(tag);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void ContainsTag_TagExists_ReturnsTrue()
        {
            // Arrange
            var tag = "TestTag";
            _registry.AddTag(tag);

            // Act
            var result = _registry.ContainsTag(tag);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void ContainsTag_TagDoesNotExist_ReturnsFalse()
        {
            // Arrange
            var tag = "NonExistentTag";

            // Act
            var result = _registry.ContainsTag(tag);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void GetAllTags_MultipleTagsAdded_ReturnsAllTags()
        {
            // Arrange
            string[] tags = { "Tag1", "Tag2", "Tag3" };
            foreach (var tag in tags)
            {
                _registry.AddTag(tag);
            }

            // Act
            var result = _registry.GetAllTags();

            // Assert
            CollectionAssert.AreEquivalent(tags, result);
        }

        [Test]
        public void GetAllTags_CacheIsUpdatedCorrectly()
        {
            // Arrange
            var tag = "Tag1";
            _registry.AddTag(tag);
            _registry.GetAllTags(); // Populate cache

            // Act
            _registry.RemoveTag(tag);
            var result = _registry.GetAllTags();

            // Assert
            Assert.IsEmpty(result);
        }

        [Test]
        public void ConcurrentAccess_AddAndRemoveTags_HandlesConcurrencyCorrectly()
        {
            // Arrange
            var tagsToAdd = Enumerable.Range(0, 100).Select(i => $"Tag{i}").ToArray();

            // Act
            Parallel.ForEach(tagsToAdd, tag => _registry.AddTag(tag));
            Parallel.ForEach(tagsToAdd, tag => _registry.RemoveTag(tag));

            // Assert
            Assert.IsEmpty(_registry.GetAllTags());
        }
    }
}
