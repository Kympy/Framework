using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace DragonGate
{
    /// <summary>
    /// 3D World Space용 LocalizedTextMeshPro (TextMeshPro 상속)
    /// UI용은 LocalizedTextMeshProUGUI 사용
    /// </summary>
    public class LocalizedTextMeshPro : TextMeshPro
    {
        public LocalizedString LocalizedStringRef => _localizedString;
        public string GetLocalizedString() => _localizedString.GetLocalizedString();

        [SerializeField] private LocalizedString _localizedString;

        protected override void OnEnable()
        {
            base.OnEnable();
            _localizedString.StringChanged -= UpdateText;
            _localizedString.StringChanged += UpdateText;
            if (!_localizedString.IsEmpty)
                _localizedString.RefreshString();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _localizedString.StringChanged -= UpdateText;
        }

        public void Refresh()
        {
            _localizedString.RefreshString();
        }

        // ── SetTextKey ─────────────────────────────────────────────────────────

        public void SetTextKey(string key)
        {
            _localizedString.TableEntryReference = key;
            if (isActiveAndEnabled)
                _localizedString.RefreshString();
        }

        public void SetTextKey(TableEntryReference key)
        {
            _localizedString.TableEntryReference = key;
            if (isActiveAndEnabled)
                _localizedString.RefreshString();
        }

        public void SetTextKey(TableReference tableReference, TableEntryReference entryReference)
        {
            _localizedString.TableReference = tableReference;
            _localizedString.TableEntryReference = entryReference;
            if (isActiveAndEnabled)
                _localizedString.RefreshString();
        }

        // ── SetArguments ───────────────────────────────────────────────────────

        public void SetArguments(params object[] args)
        {
            _localizedString.Arguments = args;
            if (isActiveAndEnabled)
                _localizedString.RefreshString();
        }

        // ── SetTextKeyWithArguments ────────────────────────────────────────────

        public void SetTextKeyWithArguments(TableEntryReference key, params object[] args)
        {
            _localizedString.TableEntryReference = key;
            _localizedString.Arguments = args;
            if (isActiveAndEnabled)
                _localizedString.RefreshString();
        }

        public void UpdateText(string value)
        {
            SetText(value);
        }
    }
}