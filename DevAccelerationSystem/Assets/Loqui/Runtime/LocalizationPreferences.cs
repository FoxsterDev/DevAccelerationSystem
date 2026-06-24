using UnityEngine;

namespace Loqui
{
    public static class LocalizationPreferences
    {
        private const string LanguageChoiceKey = "Localization.LanguageChoice";

        public static bool HasExplicitChoice => !string.IsNullOrEmpty(GetExplicitChoice());

        public static string GetExplicitChoice()
        {
            return PlayerPrefs.GetString(LanguageChoiceKey, string.Empty);
        }

        public static void SetExplicitChoice(string languageCode)
        {
            PlayerPrefs.SetString(LanguageChoiceKey, languageCode ?? string.Empty);
            PlayerPrefs.Save();
        }

        public static void ClearExplicitChoice()
        {
            PlayerPrefs.DeleteKey(LanguageChoiceKey);
            PlayerPrefs.Save();
        }
    }
}
