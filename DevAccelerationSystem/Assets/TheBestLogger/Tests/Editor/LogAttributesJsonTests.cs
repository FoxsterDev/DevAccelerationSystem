using System;
using System.Globalization;
using System.Threading;
using NUnit.Framework;
using TheBestLogger.Core.Utilities;

namespace TheBestLogger.Tests.Editor
{
    [TestFixture]
    public class LogAttributesJsonTests
    {
        private enum Trigger
        {
            EnterLobbyTab
        }

        [Test]
        public void ToSimpleJson_WritesNullBoolEnumAndNumbersAsJsonValues()
        {
            var props = new LogAttributes(5)
                .Add("trigger", Trigger.EnterLobbyTab)
                .Add("missing", null)
                .Add("flag", true)
                .Add("count", 3)
                .Add("revenue", 0.75d)
                .Props;

            Assert.That(props.ToSimpleJson(),
                Is.EqualTo("{\"trigger\":\"EnterLobbyTab\",\"missing\":null,\"flag\":true,\"count\":3,\"revenue\":0.75}"));
        }

        [Test]
        public void ToSimpleJson_WritesNullableEnumsAsNullOrQuotedName()
        {
            Trigger? none = null;
            Trigger? some = Trigger.EnterLobbyTab;
            var props = new LogAttributes(2).Add("none", none).Add("some", some).Props;

            Assert.That(props.ToSimpleJson(), Is.EqualTo("{\"none\":null,\"some\":\"EnterLobbyTab\"}"));
        }

        [Test]
        public void ToSimpleJson_EscapesQuotesBackslashesAndControlCharacters()
        {
            var props = new LogAttributes("error", "Expected \"min-max\"\\\n").Props;

            Assert.That(props.ToSimpleJson(), Is.EqualTo("{\"error\":\"Expected \\\"min-max\\\"\\\\\\n\"}"));
        }

        [Test]
        public void ToSimpleJson_FormatsNumbersWithInvariantCulture()
        {
            var previous = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
            try
            {
                var props = new LogAttributes(2).Add("revenue", 0.75d).Add("ratio", 1.5f).Props;

                Assert.That(props.ToSimpleJson(), Is.EqualTo("{\"revenue\":0.75,\"ratio\":1.5}"));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
            }
        }

        [Test]
        public void ToSimpleJson_WritesNonFiniteNumbersAsNull()
        {
            var props = new LogAttributes(3)
                .Add("nan", double.NaN)
                .Add("inf", double.PositiveInfinity)
                .Add("fnan", float.NaN)
                .Props;

            Assert.That(props.ToSimpleJson(), Is.EqualTo("{\"nan\":null,\"inf\":null,\"fnan\":null}"));
        }

        [Test]
        public void ToSimpleJson_QuotesDatesAndOtherObjectsWithInvariantFormatting()
        {
            var props = new LogAttributes(2)
                .Add("at", new DateTime(2026, 10, 5, 17, 0, 0, DateTimeKind.Utc))
                .Add("span", TimeSpan.FromMinutes(1.5))
                .Props;

            Assert.That(props.ToSimpleJson(), Is.EqualTo("{\"at\":\"10/05/2026 17:00:00\",\"span\":\"00:01:30\"}"));
        }

        [Test]
        public void ToSimpleJson_ReturnsEmptyForNullOrEmptyProps()
        {
            Assert.That(new LogAttributes().Props.ToSimpleJson(), Is.EqualTo(string.Empty));
            Assert.That(new LogAttributes(2).Props.ToSimpleJson(), Is.EqualTo(string.Empty));
        }
    }
}
