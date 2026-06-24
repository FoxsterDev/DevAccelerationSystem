using UnityEngine;

namespace Loqui
{
    public enum LocalizationPlatform
    {
        Default = 0,
        IOS = 1,
        Android = 2
    }

    public static class LocalizationPlatformResolver
    {
        public static LocalizationPlatform Current => Resolve(Application.platform);

        public static LocalizationPlatform Resolve(RuntimePlatform platform)
        {
            switch (platform)
            {
                case RuntimePlatform.IPhonePlayer:
                    return LocalizationPlatform.IOS;
                case RuntimePlatform.Android:
                    return LocalizationPlatform.Android;
                default:
                    return LocalizationPlatform.Default;
            }
        }
    }
}
