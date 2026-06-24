using System.Collections.Generic;

namespace Loqui
{
    public sealed class LanguagePickerController
    {
        public IReadOnlyList<LocalizationLanguageInfo> Options => Loc.AvailableLanguages;

        public bool IsAvailable => Loc.IsEnabled && Loc.IsReady && Options.Count > 1;

        public string CurrentLanguageCode => Loc.CurrentLanguageCode;

        public int CurrentIndex
        {
            get
            {
                var options = Options;
                var current = CurrentLanguageCode;
                for (var i = 0; i < options.Count; i++)
                {
                    if (LocalizationLanguageCodes.Equals(options[i].LanguageCode, current))
                    {
                        return i;
                    }
                }

                return -1;
            }
        }

        public bool SelectIndex(int index)
        {
            var options = Options;
            if (index < 0 || index >= options.Count)
            {
                return false;
            }

            return Loc.SetLanguage(options[index].LanguageCode);
        }

        public bool Select(string languageCode)
        {
            return Loc.SetLanguage(languageCode);
        }

        public bool ResetToSystemLanguage()
        {
            return Loc.ResetToSystemLanguage();
        }
    }
}
