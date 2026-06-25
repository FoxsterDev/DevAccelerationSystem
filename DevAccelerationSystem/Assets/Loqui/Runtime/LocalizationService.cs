using System;
using System.Collections.Generic;
using Loqui.Remote;
using TMPro;
using UnityEngine;

namespace Loqui
{
    public sealed class LocalizationService
    {
        private readonly LocalizationCatalog _catalog;
        private readonly LocalizationPlatform _platform;
        private readonly SystemLanguage _systemLanguage;
        private readonly string _defaultLanguageCode;
        private readonly ILoquiLog _logger;

        private readonly List<LocalizationEntry> _entryBuffer = new();
        private readonly List<string> _supportedCodes = new();
        private readonly List<LocalizationLanguageInfo> _availableLanguages = new();
        private readonly HashSet<string> _reportedMissingKeys = new(StringComparer.Ordinal);
        private readonly List<LocalizationBoolEntry> _boolBuffer = new();
        private readonly Dictionary<string, bool> _boolConfig = new(StringComparer.Ordinal);

        private LocalizationActiveTable _activeTable;
        private LocalizationFormatter _formatter = LocalizationFormatter.Invariant;
        private TMP_FontAsset _activeTmpFont;
        private Font _activeLegacyFont;
        private LocalizationOverridesDto _activeOverrides;

        public bool IsEnabled { get; private set; }
        public bool IsReady { get; private set; }
        public string CurrentLanguageCode { get; private set; }
        public IReadOnlyList<LocalizationLanguageInfo> AvailableLanguages => _availableLanguages;
        public TMP_FontAsset ActiveTmpFont => _activeTmpFont;

        public event Action LanguageChanged;
        public event Action Ready;

        internal LocalizationService(
            LocalizationSettingsScope settings,
            SystemLanguage systemLanguage,
            LocalizationPlatform platform,
            ILoquiLog logger = null)
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
                BuildBoolConfig();
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

        public bool TryGetBool(string key, out bool value)
        {
            if (IsEnabled && IsReady && !string.IsNullOrEmpty(key))
            {
                return _boolConfig.TryGetValue(key, out value);
            }

            value = false;
            return false;
        }

        public bool GetBool(string key, bool fallback)
        {
            return TryGetBool(key, out var value) ? value : fallback;
        }

        public bool SetLanguage(string languageCode)
        {
            if (!IsEnabled || !IsReady || !TryMatchSupported(languageCode, out var matched))
            {
                return false;
            }

            LocalizationPreferences.SetExplicitChoice(matched);
            if (LocalizationLanguageCodes.Equals(matched, CurrentLanguageCode))
            {
                return true;
            }

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

        public bool ApplyOverrides(LocalizationOverridesResult result)
        {
            if (!IsEnabled || !IsReady || result == null || !result.Accepted || result.Payload == null)
            {
                return false;
            }

            _activeOverrides = result.Payload;
            RebuildActive(CurrentLanguageCode);
            LanguageChanged?.Invoke();
            return true;
        }

        public bool ClearOverrides()
        {
            if (_activeOverrides == null)
            {
                return false;
            }

            _activeOverrides = null;
            if (IsEnabled && IsReady)
            {
                RebuildActive(CurrentLanguageCode);
                LanguageChanged?.Invoke();
            }

            return true;
        }

        public bool TryGetActiveTmpFont(out TMP_FontAsset font)
        {
            font = _activeTmpFont;
            return font != null;
        }

        public bool TryGetActiveLegacyFont(out Font font)
        {
            font = _activeLegacyFont;
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

        private void BuildBoolConfig()
        {
            _boolConfig.Clear();
            if (_catalog == null)
            {
                return;
            }

            _boolBuffer.Clear();
            _catalog.CollectBoolEntries(_boolBuffer);
            for (var i = 0; i < _boolBuffer.Count; i++)
            {
                var entry = _boolBuffer[i];
                if (entry != null && !string.IsNullOrEmpty(entry.Key) && entry.Values != null)
                {
                    _boolConfig[entry.Key] = entry.Values.Resolve(_platform);
                }
            }
        }

        private void RebuildActive(string languageCode)
        {
            CurrentLanguageCode = languageCode;
            _activeTable = LocalizationActiveTable.Build(_catalog, languageCode, _platform, _entryBuffer);
            ApplyActiveOverrides(languageCode);
            _catalog.TryGetLocale(languageCode, out var locale);
            _formatter = new LocalizationFormatter(locale?.CultureName);
            var fontProfile = locale?.FontProfile;
            _activeTmpFont = fontProfile != null ? fontProfile.ResolveTmpFont(_platform) : null;
            _activeLegacyFont = fontProfile?.LegacyFont;
            ApplyFontFallbacks(fontProfile, _activeTmpFont);
            _reportedMissingKeys.Clear();
        }

        private void ApplyActiveOverrides(string languageCode)
        {
            if (_activeOverrides?.Languages == null || _activeTable == null)
            {
                return;
            }

            for (var i = 0; i < _activeOverrides.Languages.Length; i++)
            {
                var block = _activeOverrides.Languages[i];
                if (block?.Entries == null || !LocalizationLanguageCodes.Equals(block.LanguageCode, languageCode))
                {
                    continue;
                }

                for (var j = 0; j < block.Entries.Length; j++)
                {
                    var entry = block.Entries[j];
                    if (entry == null || string.IsNullOrEmpty(entry.Key))
                    {
                        continue;
                    }

                    if (TryResolveOverrideValue(entry, out var value))
                    {
                        _activeTable.Set(entry.Key, value);
                    }
                }
            }
        }

        private bool TryResolveOverrideValue(LocalizationOverrideEntryDto entry, out string value)
        {
            switch (_platform)
            {
                case LocalizationPlatform.IOS:
                    if (!string.IsNullOrEmpty(entry.iOS))
                    {
                        value = entry.iOS;
                        return true;
                    }

                    break;
                case LocalizationPlatform.Android:
                    if (!string.IsNullOrEmpty(entry.Android))
                    {
                        value = entry.Android;
                        return true;
                    }

                    break;
            }

            if (!string.IsNullOrEmpty(entry.Default))
            {
                value = entry.Default;
                return true;
            }

            value = null;
            return false;
        }

        private static void ApplyFontFallbacks(LocalizationFontProfile profile, TMP_FontAsset font)
        {
            if (profile?.FallbackFonts == null || font == null)
            {
                return;
            }

            font.fallbackFontAssetTable ??= new List<TMP_FontAsset>();
            for (var i = 0; i < profile.FallbackFonts.Count; i++)
            {
                var fallback = profile.FallbackFonts[i];
                if (fallback != null && fallback != font && !font.fallbackFontAssetTable.Contains(fallback))
                {
                    font.fallbackFontAssetTable.Add(fallback);
                }
            }
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
