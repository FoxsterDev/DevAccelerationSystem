using System;

namespace Loqui
{
    [Serializable]
    public class LocalizationPlatformValues
    {
        public string Default;
        public string IOS;
        public string Android;

        public bool TryGet(LocalizationPlatform platform, out string value)
        {
            switch (platform)
            {
                case LocalizationPlatform.IOS:
                    if (!string.IsNullOrEmpty(IOS))
                    {
                        value = IOS;
                        return true;
                    }
                    break;
                case LocalizationPlatform.Android:
                    if (!string.IsNullOrEmpty(Android))
                    {
                        value = Android;
                        return true;
                    }
                    break;
            }

            if (!string.IsNullOrEmpty(Default))
            {
                value = Default;
                return true;
            }

            value = null;
            return false;
        }
    }
}
