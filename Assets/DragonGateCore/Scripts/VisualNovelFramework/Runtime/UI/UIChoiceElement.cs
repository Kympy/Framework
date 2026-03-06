using System;
using UnityEngine;

namespace DragonGate
{
    public class UIChoiceElement : RecycledScrollViewElement
    {
        [SerializeField] private BetterButton _button;
        [SerializeField] private LocalizedTextMeshProUGUI _text;
        
        public event Action<int> OnChoiceClicked;

        private void Awake()
        {
            _button.OnLeftClick.AddListener(() => OnChoiceClicked?.Invoke(Index));
        }

        public void SetData(ChoiceData data)
        {
            _text.LocalizedStringRef.TableReference = data.ChoiceText.TableReference;
            _text.LocalizedStringRef.TableEntryReference = data.ChoiceText.TableEntryReference;

            // Arguments 복사
            if (data.ChoiceText.Arguments != null)
            {
                var argCount = data.ChoiceText.Arguments.Count;
                _text.LocalizedStringRef.Arguments = new object[argCount];
                for (int index = 0; index < argCount; index++)
                {
                    _text.LocalizedStringRef.Arguments[index] = data.ChoiceText.Arguments[index];
                }
            }

            // SmartFormat Variables 복사
            foreach (var key in data.ChoiceText.Keys)
            {
                if (data.ChoiceText.TryGetValue(key, out var variable))
                {
                    _text.LocalizedStringRef.Add(key, variable);
                }
            }

            var localizedString = _text.GetLocalizedString();
            _text.SetText($"{Index + 1}. {localizedString}");

            _button.interactable = data.IsEnabled;
        }
    }
}
