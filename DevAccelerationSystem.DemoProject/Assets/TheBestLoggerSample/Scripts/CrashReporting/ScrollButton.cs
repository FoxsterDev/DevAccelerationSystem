using UnityEngine.Events;
using UnityEngine;
using UnityEngine.UI;

namespace TheBestLoggerSample.CrashReporting
{
    public sealed class ScrollButton : MonoBehaviour
    {
        [SerializeField]
        private Button _button;

        [SerializeField]
        private Image _background;

        [SerializeField]
        private Text _label;

        public Button Button => _button;
        public Image Background => _background;
        public Text Label => _label;

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnValidate()
        {
            EnsureReferences();
        }

        public void Bind(string markup, UnityAction onClick, ColorBlock colors, Color textColor)
        {
            EnsureReferences();
            _button.onClick.RemoveAllListeners();
            if (onClick != null)
            {
                _button.onClick.AddListener(onClick);
            }

            _button.colors = colors;
            _button.targetGraphic = _background;
            _background.color = Color.white;

            _label.supportRichText = true;
            _label.color = textColor;
            _label.text = markup;
        }

        public void ResetState(Transform parent)
        {
            EnsureReferences();
            _button.onClick.RemoveAllListeners();
            _label.text = string.Empty;
            transform.SetParent(parent, false);
            gameObject.SetActive(false);
        }

        private void EnsureReferences()
        {
            if (_button == null)
            {
                _button = GetComponent<Button>();
            }

            if (_background == null)
            {
                _background = GetComponent<Image>();
            }

            if (_label == null)
            {
                _label = GetComponentInChildren<Text>(includeInactive: true);
            }
        }
    }
}
