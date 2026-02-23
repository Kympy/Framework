using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace DragonGate
{
    public class LocalizedTextMeshProUGUI : TextMeshProUGUI
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
                _localizedString.RefreshString(); // 이미 로케일이 로드된 경우 초기 텍스트 강제 반영
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

        public void SetTextKey(string key)
        {
            _localizedString.TableEntryReference = key;
            if (isActiveAndEnabled)
                _localizedString.RefreshString();
            // 비활성 상태이면 OnEnable에서 RefreshString 처리
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

        public void UpdateText(string value)
        {
            SetText(value);
        }
    }
}