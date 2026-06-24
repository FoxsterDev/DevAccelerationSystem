using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using ILogger = TheBestLogger.ILogger;

namespace Loqui
{
    public sealed class LocalizationService
    {
        private readonly LocalizationCatalog _catalog;
        private readonly LocalizationPlatform _platform;
        private readonly SystemLanguage _systemLanguage;
        private readonly string _defaultLanguageCode;
        private readonly ILogger _logger;

        private readonly List<LocalizationEntry> _entryBuffer = new();
        private readonly List<string> _supportedCodes = new();
        private readonly List<LocalizationLanguageInfo> _availableLanguages = new();
        private readonly HashSet<string> _reportedMissingKeys = new(StringComparer.Ordinal);

        private LocalizationActiveTable _activeTable;
        private LocalizationFormatter _formatter = LocalizationFormatter.Invariant;
        private TMP_FontAsset _activeTmpFont;

        public bool IsEnabled { get; private set; }
        public bool IsReady { get; private set; }
        public string CurrentLanguageCode { get; private set; }
        public IReadOnlyList<LocalizationLanguageInfo> AvailableLanguages => _availableLanguages;
        public TMP_FontAsset ActiveTmpFont => _activeTmpFont;

        public event Action LanguageChanged;
        public event Action Ready;

        public LocalizationService(
            LocalizationSettingsScope settings,
            SystemLanguage systemLanguage,
            LocalizationPlatform platform,
            ILogger logger = null)
        {
            _logger = logger;
            _platform = platform;
            _systemLanguage = systemLanguage;
            _catalog = settings?.Catalog;
            _defaultLanguageCode = string.IsNullOrEmpty(settings?.DefaultLanguageCode)
                ? LocalizationLanguageCodes.English
                : settings.DefaultLanguageCode;
            IsEnabled = settings != null && settings.EnabledByDefault && _catalog != null;
            CurrentLanguageCode = LocalizationLanguageCodes.English;
        }

        public void Initialize()
        {
            if (IsReady)
            {
                return;
            }

            if (IsEnabled && !_catalog.IsValid(out var error))
            {
                _logger?.LogError($"[Localization] Catalog invalid; localization disabled at runtime: {error}");
                IsEnabled = false;
            }

            if (IsEnabled)
            {
                BuildAvailableLanguages();
                var resolved = LocalizationLanguageResolver.Resolve(
                    LocalizationPreferences.GetExplicitChoice(),
                    _systemLanguage,
                    _supportedCodes,
                    _defaultLanguageCode);
                RebuildActive(resolved);
            }

            IsReady = true;
            Ready?.Invoke();
        }

        public bool TryGet(string key, out string value)
        {
            if (IsEnabled && IsReady && _activeTable != null)
            {
                return _activeTable.TryGet(key, out value);
            }

            value = null;
            return false;
        }

        public string Get(string key, string fallback)
        {
            if (TryGet(key, out var value))
            {
                return value;
            }

            ReportMissingKey(key);
            return fallback;
        }

        public bool SetLanguage(string languageCode)
        {
            if (!IsEnabled || !IsReady || !TryMatchSupported(languageCode, out var matched))
            {
                return false;
            }

            LocalizationPreferences.SetExplicitChoice(matched);
            RebuildActive(matched);
            LanguageChanged?.Invoke();
            return true;
        }

        public bool ResetToSystemLanguage()
        {
            if (!IsEnabled || !IsReady)
            {
                return false;
            }

            LocalizationPreferences.ClearExplicitChoice();
            var resolved = LocalizationLanguageResolver.Resolve(
                null,
                _systemLanguage,
                _supportedCodes,
                _defaultLanguageCode);
            RebuildActive(resolved);
            LanguageChanged?.Invoke();
            return true;
        }

        public bool TryGetActiveTmpFont(out TMP_FontAsset font)
        {
            font = _activeTmpFont;
            return font != null;
        }

        public string FormatNumber(double value) => _formatter.FormatNumber(value);
        public string FormatPercent(double value) => _formatter.FormatPercent(value);
        public string FormatCurrency(decimal value, string currencyCode) => _formatter.FormatCurrency(value, currencyCode);
        public string FormatShortDate(DateTime value) => _formatter.FormatShortDate(value);
        public string FormatDateTime(DateTime value) => _formatter.FormatDateTime(value);

        private void BuildAvailableLanguages()
        {
            _supportedCodes.Clear();
            _availableLanguages.Clear();
            _catalog.CollectEnabledLanguageCodes(_supportedCodes);
            for (var i = 0; i < _supportedCodes.Count; i++)
            {
                if (_catalog.TryGetLocale(_supportedCodes[i], out var locale))
                {
                    _availableLanguages.Add(locale.ToLanguageInfo());
                }
            }
        }

        private void RebuildActive(string languageCode)
        {
            CurrentLanguageCode = languageCode;
            _activeTable = LocalizationActiveTable.Build(_catalog, languageCode, _platform, _entryBuffer);
            _catalog.TryGetLocale(languageCode, out var locale);
            _formatter = new LocalizationFormatter(locale?.CultureName);
            _activeTmpFont = locale?.FontProfile != null ? locale.FontProfile.ResolveTmpFont(_platform) : null;
            _reportedMissingKeys.Clear();
        }

        private bool TryMatchSupported(string code, out string matched)
        {
            matched = null;
            if (string.IsNullOrEmpty(code))
            {
                return false;
            }

            for (var i = 0; i < _supportedCodes.Count; i++)
            {
                if (LocalizationLanguageCodes.Equals(_supportedCodes[i], code))
                {
                    matched = _supportedCodes[i];
                    return true;
                }
            }

            return false;
        }

        private void ReportMissingKey(string key)
        {
            if (!IsEnabled || !IsReady || string.IsNullOrEmpty(key) || _logger == null)
            {
                return;
            }

            if (_reportedMissingKeys.Add(key))
            {
                _logger.LogWarning($"[Localization] Missing key '{key}' for language '{CurrentLanguageCode}'; using fallback.");
            }
        }
    }
}
