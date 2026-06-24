using Loqui.Editor;
using NUnit.Framework;

namespace Loqui.Tests
{
    [TestFixture]
    public class LocalizationKeyGeneratorHardeningTests
    {
        [Test]
        public void Generate_NonAsciiSources_ProduceDistinctKeys()
        {
            var a = LocalizationKeyGenerator.Generate("panel", "开始游戏");
            var b = LocalizationKeyGenerator.Generate("panel", "设置");
            var c = LocalizationKeyGenerator.Generate("panel", "退出");
            var cyrillic = LocalizationKeyGenerator.Generate("panel", "Привет");

            Assert.AreNotEqual(a, b);
            Assert.AreNotEqual(b, c);
            Assert.AreNotEqual(a, c);
            Assert.AreNotEqual(a, cyrillic);
            StringAssert.StartsWith("panel.", a);
        }

        [Test]
        public void Generate_NonAsciiSource_IsRepeatable()
        {
            var first = LocalizationKeyGenerator.Generate("panel", "Привет");
            var second = LocalizationKeyGenerator.Generate("panel", "Привет");
            Assert.AreEqual(first, second);
        }

        [Test]
        public void Generate_AsciiSource_Unchanged()
        {
            Assert.AreEqual("panel.play_now", LocalizationKeyGenerator.Generate("panel", "Play Now"));
        }
    }
}
