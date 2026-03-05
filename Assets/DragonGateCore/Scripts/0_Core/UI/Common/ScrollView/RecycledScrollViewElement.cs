using TMPro;
using UnityEngine;

namespace DragonGate
{
    public class RecycledScrollViewElement : MonoBehaviour
    {
        public bool Initialized { get; private set; }
        public int Index => _currentIndex;
        public int LastIndex => _lastIndex;
        public bool IsLastIndex => _currentIndex == _lastIndex;
        public RectTransform Rect => _rectTransform ?? transform as RectTransform;

        private RectTransform _rectTransform;

        private int _currentIndex;
        private int _lastIndex;

#if UNITY_EDITOR
        private TextMeshProUGUI _debugText;
        [HideInInspector] public bool DebugIndexEnabled = false;
#endif

        public void InitializeOnce(System.Action<RecycledScrollViewElement> initAction)
        {
            initAction?.Invoke(this);
            Initialized = true;
        }

        public Vector2 GetSize()
        {
            return Rect.sizeDelta;
        }

        public void SetIndex(int index)
        {
            _currentIndex = index;
#if UNITY_EDITOR
            DebugIndex(index);
#endif
        }

        public void SetLastIndex(int lastIndex)
        {
            _lastIndex = lastIndex;
        }
        
#if UNITY_EDITOR
        private void DebugIndex(int index)
        {
            if (DebugIndexEnabled == false)
            {
                _debugText.GetReference()?.SetEmpty();
                return;
            }
            if (_debugText == null)
            {
                _debugText = new GameObject("Debug").AddComponent<TextMeshProUGUI>();
                _debugText.transform.SetParent(transform);
                _debugText.rectTransform.anchoredPosition = Vector2.zero;
                _debugText.color = Color.black;
                _debugText.alignment = TextAlignmentOptions.Center;
                _debugText.rectTransform.anchorMin = Vector2.zero;
                _debugText.rectTransform.anchorMax = Vector2.one;
                _debugText.rectTransform.anchoredPosition = Vector2.zero;
                _debugText.transform.localScale = Vector3.one;
                _debugText.enableAutoSizing = true;
                _debugText.fontSizeMax = 30f;
                _debugText.raycastTarget = false;
            }
            _debugText.SetText("{0}", index);
        }
#endif
    }
}