using Cysharp.Threading.Tasks;
using DragonGate;
using UnityEngine;
using UnityEngine.UI;

namespace DragonGate
{
    [RequireComponent(typeof(RectTransform))]
    public class MasterLayoutGroup : CoreBehaviour
    {
        public enum eLayoutType { Horizontal, Vertical, Grid, None }

        public RectTransform Rect => _rectTransform ?? transform as RectTransform;
        public eLayoutType LayoutType = eLayoutType.Horizontal;

        public bool UseContentSizeFitter = true;

        [Tooltip("OnEnable 또는 Awake 시 자동으로 계산 후 Layout 관련 컴포넌트를 꺼줍니다.")]
        private bool AutoDeactivate = true;
        private RectTransform _rectTransform;
        private LayoutGroup _layoutGroup;
        private ContentSizeFitter _contentSizeFitter;
#if UNITY_EDITOR
        private eLayoutType _previousLayoutType = eLayoutType.None;

        private bool _previousUseContentSizeFitter = true;
        private bool _isInitializedOnEditor = false;
        private bool _delayCallQueued = false;
#endif

        private void Awake()
        {
            GatherComponents();
        }

        private void OnEnable()
        {
            DeactivateLater().Forget();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying || UnityEditor.EditorApplication.isPlaying || UnityEditor.EditorApplication.isCompiling) return;
            InitOnEditor();
            if (_delayCallQueued == false)
            {
                UnityEditor.EditorApplication.delayCall += CreateSafe;
            }
        }
#endif

        private void GatherComponents()
        {
            // LayoutGroup 설정
            if (TryGetComponent(out LayoutGroup layoutGroup))
            {
                if (layoutGroup is HorizontalLayoutGroup horizontal)
                {
                    LayoutType = eLayoutType.Horizontal;
                    _layoutGroup = horizontal;
                }
                else if (layoutGroup is VerticalLayoutGroup vertical)
                {
                    LayoutType = eLayoutType.Vertical;
                    _layoutGroup = vertical;
                }
                else if (layoutGroup is GridLayoutGroup gridLayoutGroup)
                {
                    LayoutType = eLayoutType.Grid;
                    _layoutGroup = gridLayoutGroup;
                }
                else
                {
                    DGDebug.LogError("Unsupported Layout Group type found. Please use HorizontalLayoutGroup, VerticalLayoutGroup, or GridLayoutGroup.");
                    LayoutType = eLayoutType.None; // 커스텀 레이아웃으로 설정
                    _layoutGroup = null;
                }
            }

            // ContentSizeFitter 설정
            if (UseContentSizeFitter)
                _contentSizeFitter = gameObject.GetOrAddComponent<ContentSizeFitter>();
        }

        public void Refresh()
        {
            StopAllCoroutines();
            DeactivateLater().Forget();
        }

        public void RefreshImmediatly()
        {
            Canvas.ForceUpdateCanvases();
        }

        private async UniTaskVoid DeactivateLater()
        {
            if (_layoutGroup != null)
            {
                _layoutGroup.enabled = true;
            }
            if (_contentSizeFitter != null)
            {
                _contentSizeFitter.enabled = true;
            }

            if (_layoutGroup != null)
            {
                _layoutGroup.CalculateLayoutInputHorizontal();
                _layoutGroup.CalculateLayoutInputVertical();
                _layoutGroup.SetLayoutHorizontal();
                _layoutGroup.SetLayoutVertical();
            }

            // 프레임 단위로 보장하기에는 하위구조가 복잡할 수 있으므로 여유있게 1초 대기 약 60프레임
            await UniTaskHelper.WaitForSeconds(this, 1f);

            if (AutoDeactivate)
            {
                if (_layoutGroup != null) _layoutGroup.enabled = false;
                if (_contentSizeFitter != null) _contentSizeFitter.enabled = false;

                enabled = false;
            }
        }

        public float GetSpacingHorizontalOrVertical()
        {
            if (_layoutGroup is HorizontalOrVerticalLayoutGroup hv)
            {
                return hv.spacing;
            }
            return 0;
        }

        public Vector2 GetSpacingGrid()
        {
            if (_layoutGroup is GridLayoutGroup grid)
            {
                return grid.spacing;
            }
            return Vector2.zero;
        }

#if UNITY_EDITOR

        private void InitOnEditor()
        {
            if (_isInitializedOnEditor) return;
            // 처음 flag 가 정해지지 않은 상태일 때 정보를 수집한다.

            // 부착된 컴포넌트가 있다면 사용중인 것이므로 Use
            if (TryGetComponent(out _contentSizeFitter))
            {
                _previousUseContentSizeFitter = true;
                UseContentSizeFitter = true;
            }
            else
            {
                _previousUseContentSizeFitter = false;
                UseContentSizeFitter = false;
            }

            if (TryGetComponent(out _layoutGroup))
            {
                if (_layoutGroup is HorizontalLayoutGroup)
                {
                    _previousLayoutType = eLayoutType.Horizontal;
                    LayoutType = eLayoutType.Horizontal;
                }
                else if (_layoutGroup is VerticalLayoutGroup)
                {
                    _previousLayoutType = eLayoutType.Vertical;
                    LayoutType = eLayoutType.Vertical;
                }
                else if (_layoutGroup is GridLayoutGroup)
                {
                    _previousLayoutType = eLayoutType.Grid;
                    LayoutType = eLayoutType.Grid;
                }
                else
                {
                    // PNLog.LogError("여기 들어올 일이 없어야함. 진짜 만약을 위해 로그를 남김.");
                    _previousLayoutType = eLayoutType.None;
                    LayoutType = eLayoutType.None; // 커스텀 레이아웃으로 설정
                }
            }
            else
            {
                _previousLayoutType = eLayoutType.None;
                LayoutType = eLayoutType.None; // 기본값 설정
            }

            _isInitializedOnEditor = true;
        }

        private void CreateSafe()
        {
            if (this == null) return;
            _delayCallQueued = true;
            CreateComponentsInEditor();
            _delayCallQueued = false;
        }

        private void CreateComponentsInEditor()
        {
            bool isLayoutTypeChanged = _previousLayoutType != LayoutType;
            if (isLayoutTypeChanged)
            {
                _previousLayoutType = LayoutType;
            }

            if (_layoutGroup == null || isLayoutTypeChanged)
            {
                if (_layoutGroup != null && isLayoutTypeChanged)
                {
                    Object.DestroyImmediate(_layoutGroup, true);
                }

                switch (LayoutType)
                {
                    case eLayoutType.Horizontal:
                        _layoutGroup = gameObject.GetOrAddComponent<BetterHorizontalLayoutGroup>();
                        break;
                    case eLayoutType.Vertical:
                        _layoutGroup = gameObject.GetOrAddComponent<BetterVerticalLayoutGroup>();
                        break;
                    case eLayoutType.Grid:
                        _layoutGroup = gameObject.GetOrAddComponent<GridLayoutGroup>();
                        break;
                }
            }

            bool isContentSizeFitterChanged = _previousUseContentSizeFitter != UseContentSizeFitter;
            if (isContentSizeFitterChanged)
            {
                _previousUseContentSizeFitter = UseContentSizeFitter;
            }

            if (UseContentSizeFitter && TryGetComponent(out _contentSizeFitter) == false)
            {
                _contentSizeFitter = gameObject.AddComponent<ContentSizeFitter>();
            }
            else if (UseContentSizeFitter == false && TryGetComponent(out _contentSizeFitter))
            {
                Object.DestroyImmediate(_contentSizeFitter, true);
            }
        }
#endif
    }
}