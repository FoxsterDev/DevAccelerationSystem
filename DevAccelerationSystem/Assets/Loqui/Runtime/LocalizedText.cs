using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Loqui
{
    [AddComponentMenu("Loqui/Localized Text")]
    [DisallowMultipleComponent]
    public sealed class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string _key;
        [SerializeField, TextArea] private string _fallback;
        [SerializeField] private TMP_Text _tmpTarget;
        [SerializeField] private Text _legacyTarget;

        private bool _subscribed;

        public string Key
        {
            get => _key;
            set
            {
                _key = value;
                Apply();
            }
        }

        private void Awake()
        {
            CacheTargets();
        }

        private void OnEnable()
        {
            HandleEnable();
        }

        private void OnDisable()
        {
            HandleDisable();
        }

        private void OnDestroy()
        {
            HandleDisable();
        }

        private void OnValidate()
        {
            CacheTargets();
        }

        internal void CacheTargets()
        {
            if (_tmpTarget == null)
            {
                _tmpTarget = GetComponent<TMP_Text>();
            }

            if (_tmpTarget == null && _legacyTarget == null)
            {
                _legacyTarget = GetComponent<Text>();
            }
        }

        internal void HandleEnable()
        {
            CacheTargets();
            Apply();
            if (!_subscribed)
            {
                Loc.LanguageChanged += Apply;
                Loc.Ready += Apply;
                _subscribed = true;
            }
        }

        internal void HandleDisable()
        {
            if (_subscribed)
            {
                Loc.LanguageChanged -= Apply;
                Loc.Ready -= Apply;
                _subscribed = false;
            }
        }

        internal void Apply()
        {
            if (_tmpTarget != null)
            {
                Loc.Apply(_tmpTarget, _key, _fallback);
            }
            else if (_legacyTarget != null)
            {
                Loc.Apply(_legacyTarget, _key, _fallback);
            }
        }
    }
}
