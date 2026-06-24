using System;
using UnityEditor;
using UnityEngine;

namespace Loqui.Editor
{
    [CustomPropertyDrawer(typeof(LocalizationLanguageCodeAttribute))]
    public sealed class LocalizationLanguageCodeDrawer : PropertyDrawer
    {
        private const float Spacing = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var line = EditorGUIUtility.singleLineHeight;
            if (property.propertyType != SerializedPropertyType.String || LocalizationLanguageCodes.IsKnown(property.stringValue))
            {
                return line;
            }

            return line * 2f + Spacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var line = EditorGUIUtility.singleLineHeight;
            var fieldRect = new Rect(position.x, position.y, position.width, line);
            var known = LocalizationLanguageCodes.All;
            var current = property.stringValue;
            var options = BuildOptions(known, current, out var selected);

            EditorGUI.BeginProperty(position, label, property);
            var picked = EditorGUI.Popup(fieldRect, label.text, selected, options);
            if (picked != selected && picked < known.Length)
            {
                property.stringValue = known[picked];
            }

            EditorGUI.EndProperty();

            if (!LocalizationLanguageCodes.IsKnown(property.stringValue))
            {
                var helpRect = new Rect(position.x, position.y + line + Spacing, position.width, line);
                var message = string.IsNullOrEmpty(property.stringValue)
                    ? "Language code is empty."
                    : $"'{property.stringValue}' is not a known language code. Add it to LocalizationLanguageCodes.";
                EditorGUI.HelpBox(helpRect, message, MessageType.Warning);
            }
        }

        private static string[] BuildOptions(string[] known, string current, out int selected)
        {
            selected = -1;
            for (var i = 0; i < known.Length; i++)
            {
                if (LocalizationLanguageCodes.Equals(known[i], current))
                {
                    selected = i;
                    break;
                }
            }

            if (selected >= 0)
            {
                return known;
            }

            var options = new string[known.Length + 1];
            Array.Copy(known, options, known.Length);
            options[known.Length] = string.IsNullOrEmpty(current) ? "<empty>" : $"{current} (unknown)";
            selected = known.Length;
            return options;
        }
    }
}
