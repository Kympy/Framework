using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DragonGate
{
    public class RecycledScrollView : ScrollRect
    {
        [SerializeField] private RecycledScrollViewElement _elementPrefab;
        [Space]
        [SerializeField] private GridConstraint _gridConstraint = GridConstraint.FixedColumnCount;
        [SerializeField] private int _constraintCount = 1;
        [SerializeField] private Vector2 _spacing = Vector2.zero;
        [SerializeField] private int _prewarmCount = 0; // how many elements to pre-create
        [SerializeField, Tooltip("Extra rows/cols to render beyond the viewport for smoothness")] private int _bufferElementCount = 1;
        #if UNITY_EDITOR
        [Space] public bool DebugIndex = false;
        #endif
        
        private Vector2 _elementSize;
        private ScrollDirectionType _directionType = ScrollDirectionType.None;
        private Stack<RecycledScrollViewElement> _elementPool = new();
        private Dictionary<int, RecycledScrollViewElement> _visibleElements = new();
        private HashSet<int> _shouldBeVisible = new();
        private HashSet<int> _toReturnElementsIndex = new();

        // === Cached layout ===
        private bool _layoutDirty = true;
        private float _cachedElementWidth;   // element size incl. spacing (x)
        private float _cachedElementHeight;  // element size incl. spacing (y)
        private int _cachedCols;
        private int _cachedRows;
        private float _cachedContentWidth;
        private float _cachedContentHeight;
        private float _cachedMaxScrollX;
        private float _cachedMaxScrollY;

        private readonly List<RecycledScrollViewElement> _runtimeSpawned = new();

        private Action<RecycledScrollViewElement> _onElementInitAction;
        private Action<RecycledScrollViewElement> _onElementUpdateAction;
        private Func<int> _getItemCount;

        public void Init(Func<int> getItemCount, Action<RecycledScrollViewElement> onElementUpdate, Action<RecycledScrollViewElement> onElementInit = null)
        {
            if (!Application.isPlaying) return;
            _getItemCount = getItemCount;
            _onElementInitAction = onElementInit;
            _onElementUpdateAction = onElementUpdate;
            if (_elementPrefab == null)
            {
                DGDebug.LogError("ElementPrefab is null");
                return;
            }

            // Determine scroll direction from base ScrollRect flags
            if (horizontal && vertical)
            {
                _directionType = ScrollDirectionType.Both;
            }
            else if (horizontal)
            {
                _directionType = ScrollDirectionType.Horizontal;
            }
            else if (vertical)
            {
                _directionType = ScrollDirectionType.Vertical;
            }

            if (content == null || viewport == null)
            {
                DGDebug.LogError("ScrollRect is missing Content or Viewport reference.");
                return;
            }

            // LayoutGroup / ContentSizeFitter 는 RecycledScrollView 의 수동 배치와 충돌하므로 자동 제거
            foreach (var lg in content.GetComponents<LayoutGroup>())
            {
                DGDebug.LogWarning($"[RecycledScrollView] Content의 {lg.GetType().Name} 을 자동 제거합니다. RecycledScrollView가 직접 배치를 관리합니다.");
                Destroy(lg);
            }
            foreach (var csf in content.GetComponents<ContentSizeFitter>())
            {
                DGDebug.LogWarning("[RecycledScrollView] Content의 ContentSizeFitter를 자동 제거합니다.");
                Destroy(csf);
            }

            // Content anchors/pivot for consistent anchoredPosition math
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(0, 1);
            content.pivot = new Vector2(0, 1);

            // Prepare pool and measure element size
            CreatePool();
            for (int i = 0; i < _prewarmCount - 1; i++)
                ReturnElement(Instantiate(_elementPrefab, content));

            _elementSize = _elementPool.Peek().GetSize();

            onValueChanged.RemoveListener(UpdateScrollRect);
            onValueChanged.AddListener(UpdateScrollRect);

            MarkLayoutDirty();
            RecalculateLayout();
            UpdateScrollRect(normalizedPosition);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            onValueChanged.RemoveListener(UpdateScrollRect);
            _onElementUpdateAction = null;
        }

        protected override void OnDisable()
        {
            // Always try to clean up runtime-spawned children when leaving play mode
            // CleanupRuntimeChildren();
            base.OnDisable();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if (!Application.isPlaying) return;
            if (viewport != null)
                MarkLayoutDirty();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            // editor-time changes to spacing/lines etc. should recalc at runtime start
            MarkLayoutDirty();
        }
#endif

        private void MarkLayoutDirty()
        {
            _layoutDirty = true;
        }

        private void RecalculateLayout()
        {
            if (!Application.isPlaying || _elementSize == Vector2.zero)
                return;

            _cachedElementWidth  = _elementSize.x + _spacing.x;
            _cachedElementHeight = _elementSize.y + _spacing.y;

            GetGridSize(out _cachedCols, out _cachedRows);

            _cachedContentWidth  = _cachedCols > 0 ? _cachedCols * _elementSize.x + Mathf.Max(0, _cachedCols - 1) * _spacing.x : 0f;
            _cachedContentHeight = _cachedRows > 0 ? _cachedRows * _elementSize.y + Mathf.Max(0, _cachedRows - 1) * _spacing.y : 0f;

            _cachedMaxScrollX = Mathf.Max(0f, _cachedContentWidth  - viewport.rect.width);
            _cachedMaxScrollY = Mathf.Max(0f, _cachedContentHeight - viewport.rect.height);

            // also push to content
            content.sizeDelta = new Vector2(_cachedContentWidth, _cachedContentHeight);

            _layoutDirty = false;
        }

        private void GetGridSize(out int cols, out int rows)
        {
            int elementCount = _getItemCount();
            switch (_gridConstraint)
            {
                case GridConstraint.FixedRowCount:
                    rows = Mathf.Max(1, _constraintCount);
                    cols = Mathf.CeilToInt(elementCount / (float)rows);
                    break;
                default: // FixedColumnCount
                    cols = Mathf.Max(1, _constraintCount);
                    rows = Mathf.CeilToInt(elementCount / (float)cols);
                    break;
            }
        }

        private void UpdateScrollRect(Vector2 _)
        {
            if (!Application.isPlaying || _elementSize == Vector2.zero) return;

            if (_layoutDirty) RecalculateLayout();

            float elementWidth = _cachedElementWidth;
            float elementHeight = _cachedElementHeight;
            int totalColumnsCount = _cachedCols;
            int totalRowsCount = _cachedRows;

            // How many columns/rows are visible in the viewport (+buffer)
            int visibleCols = Mathf.CeilToInt(viewport.rect.width / elementWidth) + _bufferElementCount;
            int visibleRows = Mathf.CeilToInt(viewport.rect.height / elementHeight) + _bufferElementCount;

            // Top-left of content is (0,0). anchoredPosition.x increases to the right, y increases downward for UI.
            float scrollX = Mathf.Max(0f, -content.anchoredPosition.x);
            float scrollY = Mathf.Max(0f, content.anchoredPosition.y);

            int firstVisibleColumnIndex = 0;
            int firstVisibleRowIndex = 0;

            if (_directionType == ScrollDirectionType.Horizontal || _directionType == ScrollDirectionType.Both)
                firstVisibleColumnIndex = Mathf.FloorToInt(scrollX / elementWidth);

            if (_directionType == ScrollDirectionType.Vertical || _directionType == ScrollDirectionType.Both)
                firstVisibleRowIndex = Mathf.FloorToInt(scrollY / elementHeight);

            firstVisibleColumnIndex = Mathf.Clamp(firstVisibleColumnIndex, 0, Mathf.Max(0, totalColumnsCount - 1));
            firstVisibleRowIndex = Mathf.Clamp(firstVisibleRowIndex, 0, Mathf.Max(0, totalRowsCount - 1));

            int lastVisibleColumnIndex = Mathf.Min(totalColumnsCount - 1, firstVisibleColumnIndex + visibleCols - 1);
            int lastVisibleRowIndex = Mathf.Min(totalRowsCount - 1, firstVisibleRowIndex + visibleRows - 1);

            // Build the set of indices that SHOULD be visible
            _shouldBeVisible.Clear();
            int elementCount = _getItemCount();
            for (int r = firstVisibleRowIndex; r <= lastVisibleRowIndex; r++)
            {
                for (int c = firstVisibleColumnIndex; c <= lastVisibleColumnIndex; c++)
                {
                    int index = r * totalColumnsCount + c;
                    if (index >= 0 && index < elementCount)
                        _shouldBeVisible.Add(index);
                }
            }

            // Return those that are no longer visible
            _toReturnElementsIndex.Clear();
            foreach (var kv in _visibleElements)
            {
                if (!_shouldBeVisible.Contains(kv.Key))
                    _toReturnElementsIndex.Add(kv.Key);
            }
            foreach (var idx in _toReturnElementsIndex)
            {
                ReturnElement(_visibleElements[idx]);
                _visibleElements.Remove(idx);
            }

            // Ensure all required items are spawned and positioned
            foreach (int index in _shouldBeVisible)
            {
                if (_visibleElements.ContainsKey(index)) continue;

                var element = PopElement();
                element.SetActive(true);
                element.SetIndex(index);
                element.SetLastIndex(elementCount);
                if (element.Initialized == false)
                    element.InitializeOnce(_onElementInitAction);
#if UNITY_EDITOR
                element.DebugIndexEnabled = DebugIndex;
#endif
                // 1차원 -> 2차원
                int row = index / totalColumnsCount;
                int col = index % totalColumnsCount;

                float x = col * elementWidth + _elementSize.x * 0.5f; // pivot 이 중앙이니 절반만큼 더함
                float y = -(row * elementHeight + _elementSize.y * 0.5f);

                var rt = element.Rect;
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(x, y);

                _visibleElements.Add(index, element);
                _onElementUpdateAction?.Invoke(element);
            }
        }

        public void Refresh()
        {
            MarkLayoutDirty();
            RecalculateLayout();
            foreach (var kv in _visibleElements)
                ReturnElement(kv.Value);
            _visibleElements.Clear();
            UpdateScrollRect(normalizedPosition);
        }

        public void MoveToInstant(int index)
        {
            if (!Application.isPlaying || _elementSize == Vector2.zero) return;
            int elementCount = _getItemCount();
            index = Mathf.Clamp(index, 0, Mathf.Max(0, elementCount - 1));
            if (_layoutDirty) RecalculateLayout();

            GetContentSize(out float contentW, out float contentH);
            GetTargetScrollForIndex(index, out float targetX, out float targetY);

            // Clamp within scrollable range
            float maxX = _cachedMaxScrollX;
            float maxY = _cachedMaxScrollY;

            targetX = Mathf.Clamp(targetX, 0f, maxX);
            targetY = Mathf.Clamp(targetY, 0f, maxY);

            // Note: ScrollRect expects negative X to move right, positive Y to move down
            content.anchoredPosition = new Vector2(-targetX, targetY);
            UpdateScrollRect(normalizedPosition);
        }

        public void MoveTo(int index)
        {
            if (!Application.isPlaying || _elementSize == Vector2.zero || !isActiveAndEnabled)
            {
                MoveToInstant(index);
                return;
            }
            int elementCount = _getItemCount();
            index = Mathf.Clamp(index, 0, Mathf.Max(0, elementCount - 1));
            if (_layoutDirty) RecalculateLayout();

            GetContentSize(out float contentW, out float contentH);
            GetTargetScrollForIndex(index, out float targetX, out float targetY);

            float maxX = _cachedMaxScrollX;
            float maxY = _cachedMaxScrollY;

            targetX = Mathf.Clamp(targetX, 0f, maxX);
            targetY = Mathf.Clamp(targetY, 0f, maxY);

            Vector2 start = content.anchoredPosition;
            Vector2 end = new Vector2(-targetX, targetY);

            StopAllCoroutines();
            StartCoroutine(SmoothScroll(start, end, 0.25f));
        }

        private void GetContentSize(out float contentWidth, out float contentHeight)
        {
            GetGridSize(out int cols, out int rows);
            float elementWidth = _elementSize.x + _spacing.x;
            float elementHeight = _elementSize.y + _spacing.y;

            contentWidth = cols > 0 ? cols * _elementSize.x + Mathf.Max(0, cols - 1) * _spacing.x : 0f;
            contentHeight = rows > 0 ? rows * _elementSize.y + Mathf.Max(0, rows - 1) * _spacing.y : 0f;
        }

        private void GetTargetScrollForIndex(int index, out float targetX, out float targetY)
        {
            if (_layoutDirty) RecalculateLayout();
            float elementWidth = _cachedElementWidth;
            float elementHeight = _cachedElementHeight;
            int totalCols = _cachedCols;
            int totalRows = _cachedRows; // (unused but kept for clarity)

            int row = 0, col = 0;

            if (totalCols <= 0) totalCols = 1;
            row = index / totalCols;
            col = index % totalCols;

            // Align the item's cell to the top/left edge of the viewport
            targetX = (_directionType == ScrollDirectionType.Horizontal || _directionType == ScrollDirectionType.Both) ? col * elementWidth : 0f;
            targetY = (_directionType == ScrollDirectionType.Vertical || _directionType == ScrollDirectionType.Both) ? row * elementHeight : 0f;
        }

        private System.Collections.IEnumerator SmoothScroll(Vector2 from, Vector2 to, float duration)
        {
            if (duration <= 0f)
            {
                content.anchoredPosition = to;
                UpdateScrollRect(normalizedPosition);
                yield break;
            }

            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / duration; // unscaled so it ignores game timeScale
                content.anchoredPosition = Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
                UpdateScrollRect(normalizedPosition);
                yield return null;
            }
            content.anchoredPosition = to;
            UpdateScrollRect(normalizedPosition);
        }

        private void CreatePool()
        {
            if (!Application.isPlaying) return;
            var obj = Instantiate(_elementPrefab, content);
            obj.gameObject.hideFlags |= HideFlags.DontSaveInEditor; // don’t persist in scene
            _runtimeSpawned.Add(obj);
            ReturnElement(obj);
        }

        private RecycledScrollViewElement PopElement()
        {
            if (_elementPool.Count > 0)
            {
                return _elementPool.Pop();
            }
            var inst = Instantiate(_elementPrefab, content);
            inst.gameObject.hideFlags |= HideFlags.DontSaveInEditor;
            _runtimeSpawned.Add(inst);
            return inst;
        }

        private void ReturnElement(RecycledScrollViewElement element)
        {
            if (element.Rect.parent as RectTransform != content)
                element.Rect.SetParent(content, false);
            _elementPool.Push(element);
            element.SetActive(false);
        }

        private void CalculateContentSize()
        {
            if (!Application.isPlaying) return;
            if (_elementSize == Vector2.zero) return;
            MarkLayoutDirty();
            RecalculateLayout();
        }

        private void CleanupRuntimeChildren()
        {
            // Destroy elements we created at runtime so they don't stick around after stopping play.
            for (int i = _runtimeSpawned.Count - 1; i >= 0; i--)
            {
                var e = _runtimeSpawned[i];
                if (e == null) continue;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    UnityEditor.Undo.DestroyObjectImmediate(e.gameObject);
                else
                    Destroy(e.gameObject);
#else
                Destroy(e.gameObject);
#endif
            }
            _runtimeSpawned.Clear();
            _visibleElements.Clear();
            _elementPool.Clear();
        }
    }

    public enum GridConstraint
    {
        FixedColumnCount, // 열 수 고정, 행 수 자동 (세로 스크롤에 적합)
        FixedRowCount,    // 행 수 고정, 열 수 자동 (가로 스크롤에 적합)
    }
}