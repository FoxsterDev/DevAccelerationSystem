using System;
using Loqui;
using NUnit.Framework;

namespace Loqui.Tests
{
    [TestFixture]
    public class LocalizationFormatterTests
    {
        private static readonly LocalizationFormatter English = new("en");
        private static readonly LocalizationFormatter Portuguese = new("pt-BR");

        [Test]
        public void FormatNumber_UsesCultureSeparators()
        {
            StringAssert.Contains("1,234", English.FormatNumber(1234.5));
            StringAssert.Contains("1.234", Portuguese.FormatNumber(1234.5));
        }

        [Test]
        public void FormatPercent_UsesCultureDecimalSeparator()
        {
            StringAssert.Contains("12.34", English.FormatPercent(0.1234));
            StringAssert.Contains("%", English.FormatPercent(0.1234));
            StringAssert.Contains("12,34", Portuguese.FormatPercent(0.1234));
        }

        [Test]
        public void FormatCurrency_AppliesSymbolAndCultureSeparators()
        {
            var usd = English.FormatCurrency(1234.56m, "USD");
            StringAssert.Contains("$", usd);
            StringAssert.Contains("1,234.56", usd);

            var brl = Portuguese.FormatCurrency(1234.56m, "BRL");
            StringAssert.Contains("R$", brl);
            StringAssert.Contains("1.234,56", brl);
        }

        [Test]
        public void FormatShortDate_DiffersByCulture()
        {
            var date = new DateTime(2026, 6, 3);

            StringAssert.Contains("2026", English.FormatShortDate(date));
            Assert.AreNotEqual(English.FormatShortDate(date), Portuguese.FormatShortDate(date));
        }

        [Test]
        public void UnknownCulture_FallsBackToInvariant()
        {
            var formatter = new LocalizationFormatter("zz-ZZ");

            Assert.AreEqual(LocalizationFormatter.Invariant.FormatNumber(1234.5), formatter.FormatNumber(1234.5));
        }
    }
}
