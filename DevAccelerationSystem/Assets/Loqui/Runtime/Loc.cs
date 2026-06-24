using System;
using System.Collections.Generic;
using Loqui.Remote;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Loqui
{
    public static class Loc
    {
        private static readonly LocalizationLanguageInfo[] EmptyLanguages = Array.Empty<LocalizationLanguageInfo>();
        private static readonly LocalizationEvent _languageChanged = new();
        private static readonly LocalizationEvent _ready = new();
        private static LocalizationService _service;
        private static ILoquiLog _logger;

        public static event Action LanguageChanged
        {
            add => _languageChanged.Add(value);
            remove => _languageChanged.Remove(value);
        }

        public static event Action Ready
        {
            add
            {
                _ready.Add(value);
                if (value != null && _service != null && _service.IsReady)
                {
                    try
                    {
                        value();
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogException(ex);
                    }
                }
            }
            remove => _ready.Remove(value);
        }

        public static bool IsEnabled => _service != null && _service.IsEnabled;
        public static bool IsReady => _service != null && _service.IsReady;

        public static string CurrentLanguageCode => _service != null ? _service.CurrentLanguageCode : LocalizationLanguageCodes.English;

        public static IReadOnlyList<LocalizationLanguageInfo> AvailableLanguages =>
            _service != null ? _service.AvailableLanguages : EmptyLanguages;

        public static LocalizationService Initialize(
            LocalizationSettingsScope settings,
            SystemLanguage systemLanguage,
            LocalizationPlatform platform,
            ILoquiLog logger = null)
        {
            Shutdown();
            LocalizationMainThread.Capture();
            _logger = logger;
            _service = new LocalizationService(settings, systemLanguage, platform, logger);
            _service.LanguageChanged += RaiseLanguageChanged;
            _service.Ready += RaiseReady;
            _service.Initialize();
            return _service;
        }

        public static void Shutdown()
        {
            if (_service == null)
            {
                return;
            }

            _service.LanguageChanged -= RaiseLanguageChanged;
            _service.Ready -= RaiseReady;
            _service = null;
            _logger = null;
        }

        public static bool TryGet(string key, out string value)
        {
            if (_service != null)
            {
                return _service.TryGet(key, out value);
            }

            value = null;
            return false;
        }

        public static string Get(string key, string fallback)
        {
            return _service != null ? _service.Get(key, fallback) : fallback;
        }

        public static bool SetLanguage(string languageCode)
        {
            LocalizationMainThread.Verify(nameof(SetLanguage), _logger);
            return _service != null && _service.SetLanguage(languageCode);
        }

        public static bool ResetToSystemLanguage()
        {
            LocalizationMainThread.Verify(nameof(ResetToSystemLanguage), _logger);
            return _service != null && _service.ResetToSystemLanguage();
        }

        public static bool ApplyOverrides(LocalizationOverridesResult result)
        {
            LocalizationMainThread.Verify(nameof(ApplyOverrides), _logger);
            return _service != null && _service.ApplyOverrides(result);
        }

        public static bool ClearOverrides()
        {
            LocalizationMainThread.Verify(nameof(ClearOverrides), _logger);
            return _service != null && _service.ClearOverrides();
        }

        public static void Apply(TMP_Text target, string key, string fallback)
        {
            if (target == null)
            {
                return;
            }

            LocalizationMainThread.Verify(nameof(Apply), _logger);
            target.text = Get(key, fallback);
            if (_service != null && _service.IsEnabled && _service.IsReady && _service.TryGetActiveTmpFont(out var font))
            {
                target.font = font;
            }
        }

        public static void Apply(Text target, string key, string fallback)
        {
            if (target == null)
            {
                return;
            }

            LocalizationMainThread.Verify(nameof(Apply), _logger);
            target.text = Get(key, fallback);
            if (_service != null && _service.IsEnabled && _service.IsReady && _service.TryGetActiveLegacyFont(out var font))
            {
                target.font = font;
            }
        }

        public static string FormatNumber(double value)
        {
            return _service != null ? _service.FormatNumber(value) : LocalizationFormatter.Invariant.FormatNumber(value);
        }

        public static string FormatCurrency(decimal value, string currencyCode)
        {
            return _service != null
                ? _service.FormatCurrency(value, currencyCode)
                : LocalizationFormatter.Invariant.FormatCurrency(value, currencyCode);
        }

        public static string FormatPercent(double value)
        {
            return _service != null ? _service.FormatPercent(value) : LocalizationFormatter.Invariant.FormatPercent(value);
        }

        public static string FormatShortDate(DateTime value)
        {
            return _service != null ? _service.FormatShortDate(value) : LocalizationFormatter.Invariant.FormatShortDate(value);
        }

        public static string FormatDateTime(DateTime value)
        {
            return _service != null ? _service.FormatDateTime(value) : LocalizationFormatter.Invariant.FormatDateTime(value);
        }

        private static void RaiseLanguageChanged()
        {
            _languageChanged.Raise(_logger);
        }

        private static void RaiseReady()
        {
            _ready.Raise(_logger);
        }
    }
}
