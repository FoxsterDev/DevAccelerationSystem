using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Loqui
{
    [AddComponentMenu("Loqui/Language Dropdown")]
    [RequireComponent(typeof(TMP_Dropdown))]
    public sealed class LanguageDropdown : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown _dropdown;
        [SerializeField] private bool _useNativeNames = true;

        private readonly LanguagePickerController _controller = new();
        private readonly List<string> _codes = new();
        private bool _subscribed;
        private bool _suppressCallback;

        private void Awake()
        {
            if (_dropdown == null)
            {
                _dropdown = GetComponent<TMP_Dropdown>();
            }
        }

        private void OnValidate()
        {
            if (_dropdown == null)
            {
                _dropdown = GetComponent<TMP_Dropdown>();
            }
        }

        private void OnEnable()
        {
            Rebuild();
            if (!_subscribed)
            {
                _dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
                Loc.LanguageChanged += SyncSelection;
                Loc.Ready += Rebuild;
                _subscribed = true;
            }
        }

        private void OnDisable()
        {
            if (_subscribed)
            {
                _dropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
                Loc.LanguageChanged -= SyncSelection;
                Loc.Ready -= Rebuild;
                _subscribed = false;
            }
        }

        private void Rebuild()
        {
            if (_dropdown == null)
            {
                return;
            }

            _codes.Clear();
            var options = new List<TMP_Dropdown.OptionData>();
            var available = _controller.Options;
            for (var i = 0; i < available.Count; i++)
            {
                var info = available[i];
                var label = _useNativeNames && !string.IsNullOrEmpty(info.NativeDisplayName)
                    ? info.NativeDisplayName
                    : info.DisplayName;
                options.Add(new TMP_Dropdown.OptionData(string.IsNullOrEmpty(label) ? info.LanguageCode : label));
                _codes.Add(info.LanguageCode);
            }

            _suppressCallback = true;
            _dropdown.ClearOptions();
            _dropdown.AddOptions(options);
            _dropdown.SetValueWithoutNotify(Mathf.Max(0, _controller.CurrentIndex));
            _dropdown.RefreshShownValue();
            _suppressCallback = false;
        }

        private void SyncSelection()
        {
            if (_dropdown == null)
            {
                return;
            }

            _dropdown.SetValueWithoutNotify(Mathf.Max(0, _controller.CurrentIndex));
            _dropdown.RefreshShownValue();
        }

        private void OnDropdownValueChanged(int index)
        {
            if (_suppressCallback)
            {
                return;
            }

            _controller.SelectIndex(index);
        }
    }
}
