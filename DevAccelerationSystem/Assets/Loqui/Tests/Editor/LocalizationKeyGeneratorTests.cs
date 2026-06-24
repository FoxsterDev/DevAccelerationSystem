using Loqui.Editor;
using NUnit.Framework;

namespace Loqui.Tests
{
    [TestFixture]
    public class LocalizationKeyGeneratorTests
    {
        [Test]
        public void Generate_IsDeterministic()
        {
            var first = LocalizationKeyGenerator.Generate("options", "Privacy Policy");
            var second = LocalizationKeyGenerator.Generate("options", "Privacy Policy");

            Assert.AreEqual(first, second);
        }

        [Test]
        public void Generate_ProducesGroupPrefixedSlug()
        {
            Assert.AreEqual("options.privacy_policy", LocalizationKeyGenerator.Generate("options", "Privacy Policy"));
        }

        [Test]
        public void Generate_CollapsesPunctuationAndWhitespace()
        {
            Assert.AreEqual("home.play_now", LocalizationKeyGenerator.Generate("home", "  Play   now!!! "));
        }

        [Test]
        public void Generate_StripsDiacritics()
        {
            Assert.AreEqual("misc.ola", LocalizationKeyGenerator.Generate("misc", "Olá"));
        }

        [Test]
        public void Generate_TruncatesToMaxWords()
        {
            Assert.AreEqual("a_b_c", LocalizationKeyGenerator.Generate(null, "a b c d e", 3));
        }

        [Test]
        public void Generate_EmptySourceFallsBackToGroup()
        {
            Assert.AreEqual("options", LocalizationKeyGenerator.Generate("options", "   "));
        }
    }
}
