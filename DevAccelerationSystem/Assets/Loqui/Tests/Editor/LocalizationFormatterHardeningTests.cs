using NUnit.Framework;

namespace Loqui.Tests
{
    [TestFixture]
    public class LocalizationFormatterHardeningTests
    {
        [Test]
        public void FormatCurrency_UsesCurrencyCountryGrouping_IndependentOfUiCulture()
        {
            var ptBr = new LocalizationFormatter("pt-BR");
            var usd = ptBr.FormatCurrency(1234.56m, "USD");

            StringAssert.Contains("$", usd);
            StringAssert.Contains("1,234.56", usd);
        }

        [Test]
        public void FormatCurrency_UnknownCurrency_FallsBackToUiCultureGrouping()
        {
            var ptBr = new LocalizationFormatter("pt-BR");
            var aud = ptBr.FormatCurrency(1234.56m, "AUD");

            StringAssert.Contains("1.234,56", aud);
            StringAssert.Contains("AUD", aud);
        }

        [Test]
        public void FormatCurrency_RepeatedCalls_AreStable()
        {
            var formatter = new LocalizationFormatter("en");
            var first = formatter.FormatCurrency(9.99m, "USD");
            var second = formatter.FormatCurrency(9.99m, "USD");

            Assert.AreEqual(first, second);
        }
    }
}
