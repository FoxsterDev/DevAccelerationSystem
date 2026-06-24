using System;
using System.Collections.Generic;
using System.Globalization;

namespace Loqui
{
    public sealed class LocalizationFormatter
    {
        public static readonly LocalizationFormatter Invariant = new(null);

        private readonly CultureInfo _culture;
        private Dictionary<string, NumberFormatInfo> _currencyFormats;

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
            return value.ToString("C", ResolveCurrencyFormat(currencyCode));
        }

        public string FormatShortDate(DateTime value)
        {
            return value.ToString("d", _culture);
        }

        public string FormatDateTime(DateTime value)
        {
            return value.ToString("g", _culture);
        }

        private NumberFormatInfo ResolveCurrencyFormat(string currencyCode)
        {
            var key = string.IsNullOrEmpty(currencyCode) ? string.Empty : currencyCode.ToUpperInvariant();
            _currencyFormats ??= new Dictionary<string, NumberFormatInfo>(StringComparer.Ordinal);
            if (_currencyFormats.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var baseCulture = ResolveCurrencyCulture(key) ?? _culture;
            var format = (NumberFormatInfo)baseCulture.NumberFormat.Clone();
            format.CurrencySymbol = ResolveCurrencySymbol(key, format.CurrencySymbol);
            _currencyFormats[key] = format;
            return format;
        }

        private static CultureInfo ResolveCurrencyCulture(string currencyCode)
        {
            string name;
            switch (currencyCode)
            {
                case "USD": name = "en-US"; break;
                case "BRL": name = "pt-BR"; break;
                case "GBP": name = "en-GB"; break;
                case "JPY": name = "ja-JP"; break;
                default: return null;
            }

            try
            {
                return CultureInfo.GetCultureInfo(name);
            }
            catch (CultureNotFoundException)
            {
                return null;
            }
        }

        private static string ResolveCurrencySymbol(string currencyCode, string fallback)
        {
            if (string.IsNullOrEmpty(currencyCode))
            {
                return fallback;
            }

            switch (currencyCode)
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
