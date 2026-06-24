using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Loqui
{
    [Serializable]
    public class LocalizationFontProfile
    {
        public TMP_FontAsset PrimaryFont;
        public List<TMP_FontAsset> FallbackFonts = new();
        public Font LegacyFont;
        public TMP_FontAsset IOSFontOverride;
        public TMP_FontAsset AndroidFontOverride;

        public bool HasTmpFont => ResolveTmpFont(LocalizationPlatform.Default) != null;

        public TMP_FontAsset ResolveTmpFont(LocalizationPlatform platform)
        {
            switch (platform)
            {
                case LocalizationPlatform.IOS:
                    if (IOSFontOverride != null)
                    {
                        return IOSFontOverride;
                    }
                    break;
                case LocalizationPlatform.Android:
                    if (AndroidFontOverride != null)
                    {
                        return AndroidFontOverride;
                    }
                    break;
            }

            return PrimaryFont;
        }
    }
}
