using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace TheBestLogger.Tests.Editor
{
    [TestFixture]
    [Timeout(5000)]
    public sealed class BackgroundFileAsyncWriterTests
    {
        private string _rootDirectory;

        [SetUp]
        public void SetUp()
        {
            _rootDirectory = Path.Combine(Path.GetTempPath(), "TheBestLoggerTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_rootDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_rootDirectory))
            {
                Directory.Delete(_rootDirectory, true);
            }
        }

        [Test]
        public void DisposeAsync_FlushesQueuedMessagesToDisk()
        {
            var fileName = "flush.txt";
            var writer = new FileBackgroundAsyncWriter(_rootDirectory, fileName);

            writer.Write("first");
            writer.Write("second");
            writer.Write("third");

            writer.DisposeAsync().GetAwaiter().GetResult();

            var lines = File.ReadAllLines(Path.Combine(_rootDirectory, fileName));
            Assert.That(lines, Is.EqualTo(new[] { "first", "second", "third" }));
        }

        [Test]
        public void HighVolumeWrites_ArePersistedWithoutLoss()
        {
            const int messageCount = 2000;
            const string fileName = "stress.txt";
            var writer = new FileBackgroundAsyncWriter(_rootDirectory, fileName);

            Parallel.For(0, messageCount, i => writer.Write("msg-" + i));

            writer.DisposeAsync().GetAwaiter().GetResult();

            var lines = File.ReadAllLines(Path.Combine(_rootDirectory, fileName));
            Assert.That(lines.Length, Is.EqualTo(messageCount));
            Assert.That(lines.Distinct().Count(), Is.EqualTo(messageCount));
        }

        [Test]
        public void ReopenSameFile_AppendsNewMessages()
        {
            const string fileName = "append.txt";

            var firstWriter = new FileBackgroundAsyncWriter(_rootDirectory, fileName);
            firstWriter.Write("first");
            firstWriter.DisposeAsync().GetAwaiter().GetResult();

            var secondWriter = new FileBackgroundAsyncWriter(_rootDirectory, fileName);
            secondWriter.Write("second");
            secondWriter.DisposeAsync().GetAwaiter().GetResult();

            var lines = File.ReadAllLines(Path.Combine(_rootDirectory, fileName));
            Assert.That(lines, Is.EqualTo(new[] { "first", "second" }));
        }

        [Test]
        public void Dispose_WhenCalledTwice_DoesNotThrow()
        {
            var writer = new FileBackgroundAsyncWriter(_rootDirectory, "double-dispose.txt");

            Assert.DoesNotThrow(writer.Dispose);
            Assert.DoesNotThrow(writer.Dispose);
        }

        [Test]
        public void DisposeAsync_WhenDirectoryCannotBeCreated_Throws()
        {
            var filePath = Path.Combine(_rootDirectory, "occupied");
            File.WriteAllText(filePath, "not a directory", Encoding.UTF8);
            var writer = new FileBackgroundAsyncWriter(filePath, "broken.txt");

            try
            {
                writer.DisposeAsync().GetAwaiter().GetResult();
                Assert.Fail("Expected IOException.");
            }
            catch (IOException)
            {
            }
        }
    }
}
