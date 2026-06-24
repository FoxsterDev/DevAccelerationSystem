using System;
using System.Globalization;

namespace Loqui
{
    public sealed class LocalizationFormatter
    {
        public static readonly LocalizationFormatter Invariant = new(null);

        private readonly CultureInfo _culture;

        public LocalizationFormatter(string cultureName)
        {
            _culture = ResolveCulture(cultureName);
        }

        public CultureInfo Culture => _culture;

        public string FormatNumber(double value)
        {
            return value.ToString("#,##0.######", _culture);
        }

        public string FormatPercent(double value)
        {
            return value.ToString("P2", _culture);
        }

        public string FormatCurrency(decimal value, string currencyCode)
        {
            var format = (NumberFormatInfo)_culture.NumberFormat.Clone();
            format.CurrencySymbol = ResolveCurrencySymbol(currencyCode, format.CurrencySymbol);
            return value.ToString("C", format);
        }

        public string FormatShortDate(DateTime value)
        {
            return value.ToString("d", _culture);
        }

        public string FormatDateTime(DateTime value)
        {
            return value.ToString("g", _culture);
        }

        private static string ResolveCurrencySymbol(string currencyCode, string fallback)
        {
            if (string.IsNullOrEmpty(currencyCode))
            {
                return fallback;
            }

            switch (currencyCode.ToUpperInvariant())
            {
                case "USD": return "$";
                case "BRL": return "R$";
                case "EUR": return "€";
                case "GBP": return "£";
                default: return currencyCode;
            }
        }

        private static CultureInfo ResolveCulture(string cultureName)
        {
            if (string.IsNullOrEmpty(cultureName))
            {
                return CultureInfo.InvariantCulture;
            }

            try
            {
                return CultureInfo.GetCultureInfo(cultureName);
            }
            catch (CultureNotFoundException)
            {
                return CultureInfo.InvariantCulture;
            }
        }
    }
}
