using System.Threading;
using NUnit.Framework;
using TheBestLogger.Core.Utilities;
using UnityEngine;

namespace TheBestLogger.Tests.Editor
{
    [TestFixture]
    public class UtilitySupplierTests
    {
        private UtilitySupplier _utilitySupplier;

        [SetUp]
        public void SetUp()
        {
            _utilitySupplier = new UtilitySupplier(20, new StackTraceFormatter(Application.dataPath, new StackTraceFormatterConfiguration()));
        }

        [Test]
        public void IsMainThread_OnMainThread_ReturnsTrue()
        {
            // Act
            var isMainThread = _utilitySupplier.IsMainThread;

            // Assert
            Assert.IsTrue(isMainThread);
        }

        [Test]
        public void GetTimeStamp_ReturnsCorrectFormattedTimestamp()
        {
            // Act
            var (currentTimeUtc, timeStampFormatted) = _utilitySupplier.GetTimeStamp();

            // Assert
            Assert.AreEqual(currentTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", System.Globalization.CultureInfo.InvariantCulture), timeStampFormatted);
        }

        [Test]
        public void GetTimeStamp_MultipleCallsWithinMinPeriod_ReturnsCachedTimestamp()
        {
            // Arrange
            var (_, firstTimeStamp) = _utilitySupplier.GetTimeStamp();
            Thread.Sleep(5);

            // Act
            var (_, secondTimeStamp) = _utilitySupplier.GetTimeStamp();

            // Assert
            Assert.AreEqual(firstTimeStamp, secondTimeStamp);
        }

        [Test]
        public void GetTimeStamp_MultipleCallsAfterMinPeriod_ReturnsUpdatedTimestamp()
        {
            // Arrange
            var (_, firstTimeStamp) = _utilitySupplier.GetTimeStamp();
            Thread.Sleep(30);

            // Act
            var (_, secondTimeStamp) = _utilitySupplier.GetTimeStamp();

            // Assert
            Assert.AreNotEqual(firstTimeStamp, secondTimeStamp);
        }

        [Test]
        public void TagsRegistry_ReturnsConcurrentTagsRegistryInstance()
        {
            // Act
            var tagsRegistry = _utilitySupplier.TagsRegistry;

            // Assert
            Assert.IsInstanceOf<ConcurrentTagsRegistry>(tagsRegistry);
        }
    }
}
