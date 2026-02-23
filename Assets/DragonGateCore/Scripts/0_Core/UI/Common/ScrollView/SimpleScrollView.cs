using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

namespace DragonGate
{

    /// <summary>
    /// 최소 구현: 뷰포트-컨텐츠 구조에서 자식 크기 합으로 content를 리사이즈하고
    /// 드래그/휠로 스크롤. LayoutElement가 있으면 preferred 사용, 없으면 rect 사용.
    /// Content의 Anchor는 (0,1), Pivot은 (0,1) (좌상단 기준)으로 맞춰 쓰는 걸 권장.
    /// </summary>
    [DisallowMultipleComponent]
    public class SimpleScrollView : UIBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        public enum Direction { Horizontal, Vertical }

        public enum Align { Start, Center, End } // Start=자식 좌/상 '모서리'를 뷰포트 '중앙'에, Center=자식 '중앙'을 뷰포트 '중앙'에, End=자식 우/하 '모서리'를 뷰포트 '중앙'에

        public RectTransform viewport => _viewport;
        public RectTransform content => _content;
        public bool IsScrolling { get; private set; } = false;

        [Header("Refs")]
        [SerializeField] private RectTransform _viewport; // 마스크/클리핑 되는 영역
        [SerializeField] private RectTransform _content; // 아이템들이 붙는 영역

        [Header("Layout")]
        public Direction direction = Direction.Horizontal;
        public RectOffset padding;
        public float spacing = 8f;
        public bool alignCenterCrossAxis = true; // 교차축(세로/가로) 중앙 정렬

        [Header("Scroll")]
        public bool inertia = true;
        [Range(0.001f, 0.999f)] public float decelerationRate = 0.135f; // ScrollRect와 동일 해석
        public float mouseWheelStep = 60f; // 휠 한 번 당 px

        private int _currentIndex = 0;
        private bool _dragging;
        private Vector2 _lastPointerPos;
        private Vector2 _velocity; // px/sec
        private Coroutine _smoothScrollRoutine;

        [Header("Index")]
        [SerializeField] private bool _autoUpdateIndex = true;
        [Tooltip("인덱스 자동 갱신 호출 간 최소 간격(초). 0이면 매 프레임 갱신")]
        [SerializeField] private float _indexUpdateInterval = 0.02f; // 50Hz 정도
        [SerializeField] private bool _snapToCenter = true;
        public event Action<int> OnIndexChanged;
        private float _lastIndexUpdateTime = -1f;

        // --- Cached children for fast access ---
        private readonly System.Collections.Generic.List<RectTransform> _children = new System.Collections.Generic.List<RectTransform>();

        /// <summary>현재 스크롤 타겟 인덱스(ScrollToIndex/SmoothScrollToIndex 호출 시 갱신)</summary>
        public int CurrentIndex => _currentIndex;

        /// <summary>현재 활성 자식 수(캐시 기준)</summary>
        public int ChildCount => _children.Count;

        /// <summary>뷰포트 중앙에 가장 가까운 자식 인덱스를 계산</summary>
        private int CalculateNearestIndex()
        {
            if (_children.Count == 0) return -1;

            if (direction == Direction.Horizontal)
            {
                // 컨텐츠 좌표계에서의 뷰포트 중앙 X
                float viewportCenterX = _viewport.rect.width * 0.5f - _content.anchoredPosition.x;

                int nearest = 0;
                float best = float.MaxValue;
                for (int i = 0; i < _children.Count; i++)
                {
                    var child = _children[i];
                    var size = GetPreferredSize(child);
                    float left = child.anchoredPosition.x - child.pivot.x * size.x;
                    float center = left + size.x * 0.5f;
                    float d = Mathf.Abs(center - viewportCenterX);
                    if (d < best) { best = d; nearest = i; }
                }
                return nearest;
            }
            else
            {
                // 컨텐츠 좌표계에서의 뷰포트 중앙 Y
                float viewportCenterY = _viewport.rect.height * 0.5f - _content.anchoredPosition.y;

                int nearest = 0;
                float best = float.MaxValue;
                for (int i = 0; i < _children.Count; i++)
                {
                    var child = _children[i];
                    var size = GetPreferredSize(child);
                    float top = -child.anchoredPosition.y - (1f - child.pivot.y) * size.y;
                    float center = top + size.y * 0.5f;
                    float d = Mathf.Abs(center - viewportCenterY);
                    if (d < best) { best = d; nearest = i; }
                }
                return nearest;
            }
        }

        /// <summary>현재 위치 기준으로 인덱스 자동 갱신(스로틀 포함)</summary>
        private void UpdateIndexByViewportPosition(bool force = false)
        {
            if (!_autoUpdateIndex || _children.Count == 0) return;

            if (!force && _indexUpdateInterval > 0f)
            {
                float now = Time.unscaledTime;
                if (_lastIndexUpdateTime >= 0f && now - _lastIndexUpdateTime < _indexUpdateInterval)
                    return;
                _lastIndexUpdateTime = now;
            }

            int idx = CalculateNearestIndex();
            if (idx >= 0 && idx != _currentIndex)
            {
                _currentIndex = idx;
                OnIndexChanged?.Invoke(_currentIndex);
            }
        }
        
        /// <summary>뷰포트 중앙에 가장 가까운 자식을 스냅(부드럽게 중앙 정렬)</summary>
        private void SnapToCenterNearest()
        {
            if (!_snapToCenter || _children.Count == 0) return;
            int idx = CalculateNearestIndex();
            if (idx < 0) return;
            SmoothScrollToIndex(idx, 0.2f, Align.Center);
        }

        // ---------- Public API ----------
        /// <summary>자식 배치와 content 리사이즈를 즉시 갱신</summary>
        public void Rebuild()
        {
            if (_viewport == null || _content == null) return;

            _content.anchorMin = new Vector2(0, 1);
            _content.anchorMax = new Vector2(0, 1);
            _content.pivot = new Vector2(0, 1);

            RebuildChildrenCache();

            // 1) 자식들의 선형 합 길이와 교차축 최대치 측정
            float mainSum = 0f;
            float crossMax = 0f;

            // 액티브 자식만(캐시 기준)
            int childCount = _children.Count;
            for (int i = 0; i < _children.Count; i++)
            {
                var child = _children[i];
                Vector2 size = GetPreferredSize(child);
                if (direction == Direction.Horizontal)
                {
                    mainSum += size.x;
                    crossMax = Mathf.Max(crossMax, size.y);
                }
                else
                {
                    mainSum += size.y;
                    crossMax = Mathf.Max(crossMax, size.x);
                }
            }

            if (childCount > 1)
                mainSum += spacing * (childCount - 1);

            // 2) 패딩 포함한 content 크기 확정
            Vector2 newSize;
            if (direction == Direction.Horizontal)
            {
                float w = padding.left + mainSum + padding.right;
                float h = Mathf.Max(_viewport.rect.height, padding.top + crossMax + padding.bottom);
                newSize = new Vector2(w, h);
            }
            else
            {
                float w = Mathf.Max(_viewport.rect.width, padding.left + crossMax + padding.right);
                float h = padding.top + mainSum + padding.bottom;
                newSize = new Vector2(w, h);
            }
            _content.sizeDelta = newSize;

            // 3) 자식 배치(좌상단 기준)
            float cursor = (direction == Direction.Horizontal) ? padding.left : padding.top; // 수직은 위→아래 양수 누적, 배치는 -cursor로

            float crossCenter = (direction == Direction.Horizontal) ? (_content.rect.height - padding.top - padding.bottom) * 0.5f : (_content.rect.width - padding.left - padding.right) * 0.5f;

            for (int i = 0; i < _children.Count; i++)
            {
                var child = _children[i];
                Vector2 size = GetPreferredSize(child);
                Vector2 anchored;

                if (direction == Direction.Horizontal)
                {
                    // 교차축(세로) 기준: topEdge = padding.top (+ 중앙 정렬 시 여분/2)
                    float availableH = _content.rect.height - padding.top - padding.bottom;
                    float topEdge = padding.top + (alignCenterCrossAxis ? Mathf.Max(0f, (availableH - size.y) * 0.5f) : 0f);
                    // pivot 보정: 앵커(좌상단) 기준 anchored.y는 'topEdge + (1 - pivotY) * height' 만큼 내려감
                    float y = -(topEdge + (1f - child.pivot.y) * size.y);
                    // 주축(가로) 기준: 좌측에서 cursor 시작, pivotX 만큼 우측으로 보정
                    float x = cursor + child.pivot.x * size.x;

                    anchored = new Vector2(x, y);
                    cursor += size.x + spacing;
                }
                else
                {
                    // 교차축(가로) 기준: leftEdge = padding.left (+ 중앙 정렬 시 여분/2)
                    float availableW = _content.rect.width - padding.left - padding.right;
                    float leftEdge = padding.left + (alignCenterCrossAxis ? Mathf.Max(0f, (availableW - size.x) * 0.5f) : 0f);
                    // pivot 보정: 앵커(좌상단) 기준 anchored.x는 'leftEdge + pivotX * width'
                    float x = leftEdge + child.pivot.x * size.x;
                    // 주축(세로) 기준: 위쪽에서 cursor 누적, pivotY 만큼 보정
                    float y = -(cursor + (1f - child.pivot.y) * size.y);

                    anchored = new Vector2(x, y);
                    cursor += size.y + spacing; // 위→아래로 배치
                }

                // anchor는 좌상단 기준으로 맞추되, pivot은 건드리지 않는다(애니 보호).
                child.anchorMin = new Vector2(0, 1);
                child.anchorMax = new Vector2(0, 1);
                child.anchoredPosition = anchored;
            }

            // 4) 컨텐츠 위치 범위 보정(뷰포트 밖 과도 이동 방지)
            _content.anchoredPosition = ClampContent(_content.anchoredPosition);
            UpdateIndexByViewportPosition(true);
        }

        /// <summary>인덱스 아이템을 뷰포트 중앙에 오도록 스크롤(즉시 이동)</summary>
        public void ScrollToIndex(int index)
        {
            if (_children.Count == 0) return;
            index = Mathf.Clamp(index, 0, _children.Count - 1);
            _content.anchoredPosition = GetIndexAnchoredPosition(index, Align.Center);
            _velocity = Vector2.zero;
            _currentIndex = index;
            OnIndexChanged?.Invoke(_currentIndex);
        }

        /// <summary>지정 인덱스를 원하는 정렬(좌/중앙/우 or 상/중앙/하)로 스크롤(즉시)</summary>
        public void ScrollToIndex(int index, Align align)
        {
            if (_children.Count == 0) return;
            index = Mathf.Clamp(index, 0, _children.Count - 1);
            _content.anchoredPosition = GetIndexAnchoredPosition(index, align);
            _velocity = Vector2.zero;
            _currentIndex = index;
            OnIndexChanged?.Invoke(_currentIndex);
        }

        public void ScrollNext(Align align = Align.Center)
        {
            if (_children.Count == 0) return;
            if (_currentIndex >= _children.Count - 1) return;
            SmoothScrollToIndex(_currentIndex + 1);
        }

        public void ScrollPrevious()
        {
            if (_children.Count == 0) return;
            if (_currentIndex <= 0) return;
            SmoothScrollToIndex(_currentIndex - 1);
        }

        /// <summary>
        /// 지정 인덱스를 뷰포트 중앙으로 부드럽게 스크롤한다.
        /// 기존 구조 변경 없이, 코루틴으로 content.anchoredPosition만 보간한다.
        /// </summary>
        public void SmoothScrollToIndex(int index, float duration = 0.2f, AnimationCurve curve = null)
        {
            if (_viewport == null || _content == null) return;
            if (_children.Count == 0) return;
            index = Mathf.Clamp(index, 0, _children.Count - 1);
            if (duration <= 0f)
            {
                _content.anchoredPosition = GetIndexAnchoredPosition(index, Align.Center);
                return;
            }
            var target = GetIndexAnchoredPosition(index, Align.Center);
            if (_smoothScrollRoutine != null) StopCoroutine(_smoothScrollRoutine);
            _velocity = Vector2.zero; // 관성 영향 제거
            _currentIndex = index;
            OnIndexChanged?.Invoke(_currentIndex);
            _smoothScrollRoutine = StartCoroutine(CoSmoothScroll(target, duration, curve));
        }

        /// <summary>지정 인덱스를 원하는 정렬(좌/중앙/우 or 상/중앙/하)로 부드럽게 스크롤</summary>
        public void SmoothScrollToIndex(int index, float duration, Align align, AnimationCurve curve = null)
        {
            if (_viewport == null || _content == null) return;
            if (_children.Count == 0) return;
            index = Mathf.Clamp(index, 0, _children.Count - 1);
            if (duration <= 0f)
            {
                _content.anchoredPosition = GetIndexAnchoredPosition(index, align);
                return;
            }
            var target = GetIndexAnchoredPosition(index, align);
            if (_smoothScrollRoutine != null) StopCoroutine(_smoothScrollRoutine);
            _velocity = Vector2.zero; // 관성 영향 제거
            _currentIndex = index;
            OnIndexChanged?.Invoke(_currentIndex);
            _smoothScrollRoutine = StartCoroutine(CoSmoothScroll(target, duration, curve));
        }

        /// <summary>index 아이템을 align 기준(Start/Center/End)에 맞춰 배치하기 위해 필요한 content.anchoredPosition 반환</summary>
        public Vector2 GetIndexAnchoredPosition(int index, Align align)
        {
            var child = GetChildAt(index);
            if (child == null) return _content.anchoredPosition;

            var size = GetPreferredSize(child);
            var targetPos = _content.anchoredPosition;

            if (direction == Direction.Horizontal)
            {
                // 좌상단 앵커 기준: child의 왼쪽 가장자리(컨텐츠 좌상단에서 오른쪽으로 +)
                float left = child.anchoredPosition.x - child.pivot.x * size.x;
                float right = left + size.x;
                float center = (left + right) * 0.5f;
                float vCenter = _viewport.rect.width * 0.5f; // viewport center (x)

                switch (align)
                {
                    case Align.Start: // child's LEFT to viewport CENTER
                        targetPos.x = -Mathf.Round(left - vCenter);
                        break;
                    case Align.Center: // child's CENTER to viewport CENTER
                        targetPos.x = -Mathf.Round(center - vCenter);
                        break;
                    case Align.End: // child's RIGHT to viewport CENTER
                        targetPos.x = -Mathf.Round(right - vCenter);
                        break;
                }
            }
            else
            {
                // 위로 갈수록 -, 아래로 갈수록 + 인 down-positive로 환산
                float top = -child.anchoredPosition.y - (1f - child.pivot.y) * size.y; // 컨텐츠 상단에서 아래로 내려온 거리
                float bottom = top + size.y;
                float center = (top + bottom) * 0.5f;
                float vCenterY = _viewport.rect.height * 0.5f; // viewport center (y)

                switch (align)
                {
                    case Align.Start: // child's TOP to viewport CENTER
                        targetPos.y = -Mathf.Round(top - vCenterY);
                        break;
                    case Align.Center: // child's CENTER to viewport CENTER
                        targetPos.y = -Mathf.Round(center - vCenterY);
                        break;
                    case Align.End: // child's BOTTOM to viewport CENTER
                        targetPos.y = -Mathf.Round(bottom - vCenterY);
                        break;
                }
            }
            return ClampContent(targetPos);
        }

        private System.Collections.IEnumerator CoSmoothScroll(Vector2 target, float duration, AnimationCurve curve)
        {
            IsScrolling = true;
            _velocity = Vector2.zero;
            var start = _content.anchoredPosition;
            start = ClampContent(start);
            float t = 0f;
            var c = curve ?? AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            duration = Mathf.Max(0.0001f, duration);

            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / duration;
                float e = c.Evaluate(Mathf.Clamp01(t));
                var pos = Vector2.Lerp(start, target, e);
                _content.anchoredPosition = ClampContent(pos);
                UpdateIndexByViewportPosition();
                yield return null;
            }
            _content.anchoredPosition = ClampContent(target);
            UpdateIndexByViewportPosition(true);
            _smoothScrollRoutine = null;
            IsScrolling = false;
        }

        // ---------- Events ----------
        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragging = true;
            _velocity = Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, eventData.position, eventData.pressEventCamera, out _lastPointerPos);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, eventData.position, eventData.pressEventCamera, out var localPos))
                return;

            var delta = localPos - _lastPointerPos;
            _lastPointerPos = localPos;

            // 뷰포트 좌표계 기준 드래그 → 컨텐츠 이동 반전
            var move = _content.anchoredPosition;
            if (direction == Direction.Horizontal)
            {
                move.x += delta.x;
                _velocity = new Vector2(delta.x / Mathf.Max(Time.unscaledDeltaTime, 0.0001f), 0);
            }
            else
            {
                move.y += delta.y;
                _velocity = new Vector2(0, delta.y / Mathf.Max(Time.unscaledDeltaTime, 0.0001f));
            }
            _content.anchoredPosition = ClampContent(move);
            UpdateIndexByViewportPosition();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _dragging = false;
            if (_snapToCenter && !IsScrolling)
            {
                SnapToCenterNearest();
            }
        }

        public void OnScroll(PointerEventData eventData)
        {
            var move = _content.anchoredPosition;
            float delta = eventData.scrollDelta.y * mouseWheelStep; // 위(+), 아래(-)
            if (direction == Direction.Horizontal)
            {
                move.x -= delta; // 휠 위=왼쪽 이동 느낌으로 반전
            }
            else
            {
                move.y += delta;
            }
            _content.anchoredPosition = ClampContent(move);
            UpdateIndexByViewportPosition();
            _velocity = Vector2.zero; // 휠은 관성 제거
        }

        // ---------- Mono ----------
        protected override void OnEnable()
        {
            base.OnEnable();
            Rebuild();
        }

        void LateUpdate()
        {
            if (_viewport == null || _content == null) return;

            if (!_dragging && inertia && !IsScrolling)
            {
                if (direction == Direction.Horizontal)
                {
                    _velocity.x *= Mathf.Pow(1f - decelerationRate, Time.unscaledDeltaTime * 60f);
                    if (Mathf.Abs(_velocity.x) > 0.01f)
                    {
                        var pos = _content.anchoredPosition;
                        pos.x += _velocity.x * Time.unscaledDeltaTime;
                        _content.anchoredPosition = ClampContent(pos);
                        UpdateIndexByViewportPosition();
                        
                        if (_snapToCenter && !IsScrolling && Mathf.Abs(_velocity.x) <= 10f)
                        {
                            _velocity.x = 0f;
                            SnapToCenterNearest();
                        }
                    }
                    else _velocity.x = 0;
                }
                else
                {
                    _velocity.y *= Mathf.Pow(1f - decelerationRate, Time.unscaledDeltaTime * 60f);
                    if (Mathf.Abs(_velocity.y) > 0.01f)
                    {
                        var pos = _content.anchoredPosition;
                        pos.y += _velocity.y * Time.unscaledDeltaTime;
                        _content.anchoredPosition = ClampContent(pos);
                        UpdateIndexByViewportPosition();
                        
                        if (_snapToCenter && !IsScrolling && Mathf.Abs(_velocity.y) <= 10f)
                        {
                            _velocity.y = 0f;
                            SnapToCenterNearest();
                        }
                    }
                    else _velocity.y = 0;
                }
            }
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            // 뷰포트 사이즈가 바뀌면 재계산 필요
            if (isActiveAndEnabled) Rebuild();
        }

        // ---------- Internals ----------
        private Vector2 GetPreferredSize(RectTransform rt)
        {
            return new Vector2(rt.rect.width, rt.rect.height);
        }

        private Vector2 ClampContent(Vector2 anchored)
        {
            // content가 viewport보다 작으면 가운데 정렬 느낌으로 고정
            if (direction == Direction.Horizontal)
            {
                float contentWidth = _content.rect.width;
                float viewportWidth = _viewport.rect.width;

                if (contentWidth <= viewportWidth)
                {
                    anchored.x = 0f; // 좌상단 기준일 때 0이 중앙 정렬처럼 보이려면 배치가 좌우 padding으로 이미 맞춰짐
                }
                else
                {
                    // 좌상단 기준: x 증가 = 오른쪽으로 이동(왼쪽 빈틈)
                    float min = -(contentWidth - viewportWidth); // 가장 오른쪽 끝
                    float max = 0f; // 가장 왼쪽 끝
                    anchored.x = Mathf.Clamp(anchored.x, min, max);
                }
            }
            else
            {
                float contentHeight = _content.rect.height;
                float viewportHeight = _viewport.rect.height;

                // 좌상단 기준: y는 위로 갈수록 +, 아래로 갈수록 -
                float min = -(contentHeight - viewportHeight); // 가장 아래로 스크롤된 상태
                float max = 0f; // 가장 위
                anchored.y = Mathf.Clamp(anchored.y, min, max);
            }
            return anchored;
        }

        private Vector2 GetIndexCenterAnchoredPosition(int index)
        {
            // For backward compatibility; always center
            return GetIndexAnchoredPosition(index, Align.Center);
        }

        /// <summary>_content의 활성 RectTransform 자식들을 순서대로 캐싱</summary>
        private void RebuildChildrenCache()
        {
            _children.Clear();
            if (_content == null) return;
            for (int i = 0; i < _content.childCount; i++)
            {
                var rt = _content.GetChild(i) as RectTransform;
                if (rt != null && rt.gameObject.activeSelf)
                    _children.Add(rt);
            }
            // 인덱스가 캐시 범위를 벗어나면 보정
            if (_currentIndex >= _children.Count) _currentIndex = Mathf.Max(0, _children.Count - 1);
        }

        /// <summary>캐시에서 인덱스 자식을 반환(없으면 null)</summary>
        private RectTransform GetChildAt(int index)
        {
            return (index >= 0 && index < _children.Count) ? _children[index] : null;
        }
    }
}