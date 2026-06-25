using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Loqui.Editor
{
    [CustomPropertyDrawer(typeof(LocalizationKeyAttribute))]
    public sealed class LocalizationKeyDrawer : PropertyDrawer
    {
        const float Spacing = 2f;
        const float ToggleWidth = 26f;

        static readonly HashSet<string> RawEditing = new();
        static readonly Dictionary<string, AdvancedDropdownState> States = new();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var line = EditorGUIUtility.singleLineHeight;
            if (property.propertyType != SerializedPropertyType.String)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }

            var unknown = !string.IsNullOrEmpty(property.stringValue) && !LocalizationKeyIndex.ContainsTextKey(property.stringValue);
            return unknown ? line * 2f + Spacing : line;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            var line = EditorGUIUtility.singleLineHeight;
            var row = new Rect(position.x, position.y, position.width, line);

            EditorGUI.BeginProperty(position, label, property);

            var fieldRect = EditorGUI.PrefixLabel(row, label);
            var inputRect = new Rect(fieldRect.x, fieldRect.y, fieldRect.width - ToggleWidth - Spacing, line);
            var toggleRect = new Rect(fieldRect.xMax - ToggleWidth, fieldRect.y, ToggleWidth, line);

            var id = KeyId(property);
            var rawEditing = RawEditing.Contains(id);

            if (rawEditing)
            {
                property.stringValue = EditorGUI.TextField(inputRect, property.stringValue);
            }
            else
            {
                var current = string.IsNullOrEmpty(property.stringValue) ? "<none>" : property.stringValue;
                if (EditorGUI.DropdownButton(inputRect, new GUIContent(current, current), FocusType.Keyboard))
                {
                    ShowDropdown(inputRect, property);
                }
            }

            var toggled = GUI.Toggle(toggleRect, rawEditing, new GUIContent("✎", "Type a raw key"), EditorStyles.miniButton);
            if (toggled != rawEditing)
            {
                if (toggled)
                {
                    RawEditing.Add(id);
                }
                else
                {
                    RawEditing.Remove(id);
                }
            }

            if (!string.IsNullOrEmpty(property.stringValue) && !LocalizationKeyIndex.ContainsTextKey(property.stringValue))
            {
                var help = new Rect(position.x, position.y + line + Spacing, position.width, line);
                EditorGUI.HelpBox(help, $"'{property.stringValue}' is not in any catalog — runtime uses the fallback.", MessageType.None);
            }

            EditorGUI.EndProperty();
        }

        static string KeyId(SerializedProperty property)
        {
            return property.serializedObject.targetObject.GetInstanceID() + ":" + property.propertyPath;
        }

        static void ShowDropdown(Rect rect, SerializedProperty property)
        {
            LocalizationKeyIndex.Refresh();
            var id = KeyId(property);
            if (!States.TryGetValue(id, out var state))
            {
                state = new AdvancedDropdownState();
                States[id] = state;
            }

            var serializedObject = property.serializedObject;
            var path = property.propertyPath;
            var dropdown = new KeyAdvancedDropdown(state, key =>
            {
                serializedObject.Update();
                var picked = serializedObject.FindProperty(path);
                if (picked != null)
                {
                    picked.stringValue = key;
                    serializedObject.ApplyModifiedProperties();
                }
            });
            dropdown.Show(rect);
        }

        sealed class KeyAdvancedDropdown : AdvancedDropdown
        {
            readonly Action<string> _onPick;
            readonly Dictionary<int, string> _idToKey = new();

            public KeyAdvancedDropdown(AdvancedDropdownState state, Action<string> onPick) : base(state)
            {
                _onPick = onPick;
                minimumSize = new Vector2(300f, 360f);
            }

            protected override AdvancedDropdownItem BuildRoot()
            {
                var root = new AdvancedDropdownItem("Localization Keys");
                _idToKey.Clear();

                var none = new AdvancedDropdownItem("(none)") { id = 0 };
                _idToKey[0] = string.Empty;
                root.AddChild(none);

                var groups = new Dictionary<string, AdvancedDropdownItem>(StringComparer.Ordinal);
                var nextId = 1;
                foreach (var entry in LocalizationKeyIndex.Entries)
                {
                    if (entry.IsBool)
                    {
                        continue;
                    }

                    var groupName = string.IsNullOrEmpty(entry.Group) ? "(ungrouped)" : entry.Group;
                    if (!groups.TryGetValue(groupName, out var group))
                    {
                        group = new AdvancedDropdownItem(groupName);
                        root.AddChild(group);
                        groups[groupName] = group;
                    }

                    var item = new AdvancedDropdownItem(entry.Key) { id = nextId };
                    _idToKey[nextId] = entry.Key;
                    nextId++;
                    group.AddChild(item);
                }

                return root;
            }

            protected override void ItemSelected(AdvancedDropdownItem item)
            {
                if (_idToKey.TryGetValue(item.id, out var key))
                {
                    _onPick(key);
                }
            }
        }
    }
}
