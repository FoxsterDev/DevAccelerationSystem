using System;

namespace Loqui
{
    public enum LocalizationBoolOverride
    {
        Inherit = 0,
        True = 1,
        False = 2
    }

    [Serializable]
    public class LocalizationBoolValues
    {
        public bool Default;
        public LocalizationBoolOverride IOS = LocalizationBoolOverride.Inherit;
        public LocalizationBoolOverride Android = LocalizationBoolOverride.Inherit;

        public bool Resolve(LocalizationPlatform platform)
        {
            LocalizationBoolOverride ovr;
            switch (platform)
            {
                case LocalizationPlatform.IOS:
                    ovr = IOS;
                    break;
                case LocalizationPlatform.Android:
                    ovr = Android;
                    break;
                default:
                    ovr = LocalizationBoolOverride.Inherit;
                    break;
            }

            switch (ovr)
            {
                case LocalizationBoolOverride.True:
                    return true;
                case LocalizationBoolOverride.False:
                    return false;
                default:
                    return Default;
            }
        }
    }
}
