using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.Localization.Tables;

namespace DragonGate
{
    public class LocalizedTextMeshProUGUI : TextMeshProUGUI
    {
        public LocalizedString LocalizedStringRef => _localizedString;
        public string GetLocalizedString() => _localizedString.GetLocalizedString();

        [SerializeField] private LocalizedString _localizedString;

        private bool _updateEventRegistered = false;

        protected override void OnEnable()
        {
            base.OnEnable();
#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif
            // 변수가 등록되어 있지 않은 경우에만 자동 refresh.
            // 변수가 필요한 포맷 스트링({ItemName} 등)은 SetData() → Refresh()에서 처리.
            if (_localizedString.IsEmpty == false)
            {
                var currentTable = LocalizationSettings.StringDatabase.GetTable(_localizedString.TableReference);
                var entry = currentTable.GetEntryFromReference(_localizedString.TableEntryReference);
                if (entry == null)
                {
                    return;
                }

                if (entry.IsSmart)
                {
                    // 스마트 스트링은 리턴
                    return;
                }
            }

            RegisterUpdateEvent();
            _localizedString.RefreshString();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _localizedString.StringChanged -= UpdateText;
            _updateEventRegistered = false;
        }

        public void Refresh()
        {
            RegisterUpdateEvent();
            _localizedString.RefreshString();
        }

        // ── SetTextKey ─────────────────────────────────────────────────────────

        public void SetTextKey(string key)
        {
            _localizedString.TableEntryReference = key;
            RegisterUpdateEvent();
            if (isActiveAndEnabled)
                _localizedString.RefreshString();
            // 비활성 상태이면 OnEnable에서 RefreshString 처리
        }

        public void SetTextKey(TableEntryReference key)
        {
            _localizedString.TableEntryReference = key;
            RegisterUpdateEvent();
            if (isActiveAndEnabled)
                _localizedString.RefreshString();
        }

        public void SetTextKey(TableReference tableReference, TableEntryReference entryReference)
        {
            _localizedString.TableReference = tableReference;
            _localizedString.TableEntryReference = entryReference;
            RegisterUpdateEvent();
            if (isActiveAndEnabled)
                _localizedString.RefreshString();
        }

        public void SetCopy(LocalizedString copy)
        {
            _localizedString.TableReference = copy.TableReference;
            _localizedString.TableEntryReference = copy.TableEntryReference;

            // Arguments 복사
            if (copy.Arguments != null)
            {
                var argCount = copy.Arguments.Count;
                _localizedString.Arguments = new object[argCount];
                for (int index = 0; index < argCount; index++)
                {
                    _localizedString.Arguments[index] = copy.Arguments[index];
                }
            }

            // SmartFormat Variables 복사
            foreach (var key in copy.Keys)
            {
                if (copy.TryGetValue(key, out var variable))
                {
                    _localizedString.Add(key, variable);
                }
            }
            _localizedString.RefreshString();
        }

        // ── SetArguments ───────────────────────────────────────────────────────

        public void SetArguments(params object[] args)
        {
            _localizedString.Arguments = args;
            RegisterUpdateEvent();
            if (isActiveAndEnabled)
                _localizedString.RefreshString();
        }

        // ── SetTextKeyWithArguments ────────────────────────────────────────────
        // 키와 인자를 한 번에 설정해 RefreshString 호출을 1회로 줄임

        public void SetTextKeyWithArguments(TableEntryReference key, params object[] args)
        {
            _localizedString.TableEntryReference = key;
            _localizedString.Arguments = args;
            RegisterUpdateEvent();
            if (isActiveAndEnabled)
                _localizedString.RefreshString();
        }

        public void SetVariable(string key, int value)
        {
            _localizedString[key] = new IntVariable() { Value = value };
        }

        public void SetVariable(string key, float value)
        {
            _localizedString[key] = new FloatVariable() { Value = value };
        }

        public void SetVariable(string key, long value)
        {
            _localizedString[key] = new LongVariable() { Value = value };
        }

        public void SetVariable(string key, char value)
        {
            _localizedString[key] = new StringVariable() { Value = value.ToString() };
        }

        public void SetVariable(string key, bool value)
        {
            _localizedString[key] = new BoolVariable() { Value = value };
        }

        public void SetVariable(string key, string value)
        {
            _localizedString[key] = new StringVariable() { Value = value };
        }

        public void SetVariable(string key, LocalizedString value)
        {
            _localizedString[key] = value;
        }

        public void UpdateText(string value)
        {
            SetText(value);
        }

        public void Clear()
        {
            SetText("");
        }

        private void RegisterUpdateEvent()
        {
            if (_updateEventRegistered) return;
            _localizedString.StringChanged -= UpdateText;
            _localizedString.StringChanged += UpdateText;
            _updateEventRegistered = true;
        }
    }
}
