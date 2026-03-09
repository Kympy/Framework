using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DragonGate
{
    public class PopupController
    {
        private GameObject _rootObject;
        private LinkedList<PopupCore> _activePopups = new();
        private Stack<PopupContainer> _containerPool = new();
        private Dictionary<int, int> _countInSortOrder = new();

        // 풀링
        private Dictionary<Type, Stack<GameObject>> _pool = new();
        private Dictionary<string, Type> _keyToType = new(); // 타입 캐싱
        private PopupContainer _popupContainer;

        private const string ContainerPrefabKey = "UI/Common/PopupContainer";

        public void Init()
        {
            _rootObject ??= new GameObject("Popup");
            Object.DontDestroyOnLoad(_rootObject);
        }

        public PopupCore Show(string key) => Show(key, 0);
        public T Show<T>(string key) where T : PopupCore => Show(key) as T;
        public PopupCore Show(PopupCore instance) => Show(instance, 0);
        public T Show<T>(PopupCore instance) where T : PopupCore => Show(instance) as T;

        public PopupCore Show<ViewState>(PopupCore instance, ViewState viewState) where ViewState : struct
        {
            // 이미 활성 중이면 상태만 업데이트
            if (_activePopups.Contains(instance))
            {
                if (instance is IViewState<ViewState> sv) sv.SetViewState(in viewState);
                return instance;
            }

            // 풀에 있으면 제거 후 재사용
            RemoveFromPool(instance);
            instance.gameObject.SetActive(true);
            return ShowPopupInternal(instance, viewState);
        }

        public PopupCore Show<ViewState>(string key, ViewState viewState) where ViewState : struct
        {
            // 1. 타입 확인 (캐시 또는 첫 로드)
            if (!_keyToType.TryGetValue(key, out Type popupType))
            {
                // 첫 로드: 타입 확인 후 캐싱
                var tempPopup = LoadAsset(key);
                if (tempPopup == null) return null;
                popupType = tempPopup.GetType();
                _keyToType[key] = popupType;

                // 첫 로드는 그대로 표시
                return ShowPopupInternal(tempPopup, viewState);
            }

            // 2. 타입 캐시 있음 - AllowMultipleInstance 확인
            var activePopup = FindActivePopup(popupType);
            if (activePopup != null && !activePopup.AllowMultipleInstance)
            {
                DGDebug.Log($"PopupController : Reuse existing ({key})", Color.cyan);

                // 상태만 업데이트
                if (activePopup is IViewState<ViewState> stateView)
                {
                    stateView.SetViewState(in viewState);
                }
                return activePopup;
            }

            // 3. 다중 인스턴스 허용 또는 활성 팝업 없음 - 새로 로드
            var popup = LoadPopup(key);
            if (popup == null) return null;

            return ShowPopupInternal(popup, viewState);
        }

        private PopupCore ShowPopupInternal<ViewState>(PopupCore popup, ViewState viewState) where ViewState : struct
        {
            DGDebug.Log($"PopupController : Show ({popup.GetType().Name})", Color.cyan);
            popup.SetVisible();
            AttachPopup(popup);

            // 렌더 상태 적용
            if (popup is IViewState<ViewState> stateView)
            {
                stateView.SetViewState(in viewState);
            }
            return popup;
        }

        private PopupCore FindActivePopup(Type popupType)
        {
            foreach (var popup in _activePopups)
            {
                if (popup != null && popup.GetType() == popupType)
                {
                    return popup;
                }
            }
            return null;
        }

        public T Show<T, ViewState>(string key, ViewState viewState) where ViewState : struct where T : PopupCore
        {
            return Show(key, viewState) as T;
        }

        public void Hide(string key)
        {
            if (!_keyToType.TryGetValue(key, out var type)) return;
            var popup = FindActivePopup(type);
            if (popup != null) Hide(popup);
        }

        public void Hide(PopupCore popup)
        {
            if (popup == null) return;

            popup.SetHidden(() =>
            {
                DetachPopup(popup);
                ReturnToPool(popup);
            });
        }
        
        public void AttachPopup(PopupCore popup)
        {
            var container = GetOrCreateContainer();
            container.SetPopup(popup);
            var currentLayerPopupCount = _countInSortOrder.GetValueOrDefault(popup.DefaultSortOrder, 0);
            container.SetSortOrder(popup.DefaultSortOrder + currentLayerPopupCount);
            popup.SetContainer(container);
            // 활성화 등록
            _activePopups.AddLast(popup);
            // 현재 레이어의 카운트 증가
            IncreaseLayerPopupCount(popup.DefaultSortOrder);
        }

        public void DetachPopup(PopupCore popup)
        {
            var currentNode = _activePopups.Last;
            while (currentNode != null)
            {
                if (currentNode.Value == popup)
                    break;
                currentNode = currentNode.Previous;
            }
            if (currentNode == null) return;
            // 팝업 리스트에서 제거
            _activePopups.Remove(currentNode);
            DecreaseLayerPopupCount(popup.DefaultSortOrder);
            // 컨테이너를 반환하며 팝업을 분리
            ReturnContainer(currentNode.Value.Container);
        }

        public void HideAll()
        {
            var popups = new List<PopupCore>(_activePopups);
            foreach (var popup in popups)
            {
                if (popup == null) continue;
                Hide(popup);
            }
        }

        public void SetViewState<TViewState>(System.Type targetType, in TViewState viewState) where TViewState : struct
        {
            var popup = FindActivePopup(targetType);
            if (popup is IViewState<TViewState> stateView)
                stateView.SetViewState(in viewState);
        }

        public bool HasKey(string key) => _keyToType.ContainsKey(key);

        public bool IsVisible(string key)
        {
            if (!_keyToType.TryGetValue(key, out var type)) return false;
            return FindActivePopup(type) != null;
        }

        public bool HasPopup()
        {
            return _activePopups.Count > 0;
        }

        public PopupCore GetTopPopup()
        {
            if (_activePopups.Count == 0) return null;
            return _activePopups.Last.Value;
        }

        // 팝업을 담을 컨테이너를 생성
        private PopupContainer GetOrCreateContainer()
        {
            if (_containerPool.TryPop(out var container))
            {
                container.SetActive(true);
                return container;
            }

            if (_popupContainer == null)
            {
                var loaded = Resources.Load<GameObject>(ContainerPrefabKey);
                _popupContainer = loaded.GetComponent<PopupContainer>();
            }
            var newContainer = Object.Instantiate(_popupContainer, _rootObject.transform);
            newContainer.Init(_rootObject);
            return newContainer;
        }

        private void ReturnContainer(PopupContainer container)
        {
            container.SetPopup(null);
            _containerPool.Push(container);
            container.SetActive(false);
        }
        
        private void IncreaseLayerPopupCount(int popupSortOrder)
        {
            if (_countInSortOrder.ContainsKey(popupSortOrder))
            {
                _countInSortOrder[popupSortOrder] += 1;
            }
            else
            {
                _countInSortOrder.Add(popupSortOrder, 1);
            }
        }

        private void DecreaseLayerPopupCount(int popupSortOrder)
        {
            if (_countInSortOrder.ContainsKey(popupSortOrder))
            {
                _countInSortOrder[popupSortOrder] -= 1;
            }
        }

        // 팝업 로드 (풀에서 꺼내거나 새로 생성)
        private PopupCore LoadPopup(string key)
        {
            // 1. 타입 캐시 확인
            if (!_keyToType.TryGetValue(key, out Type popupType))
            {
                // 첫 로드: 타입 확인 후 캐싱
                var tempPopup = LoadAsset(key);
                if (tempPopup == null) return null;
                popupType = tempPopup.GetType();
                _keyToType[key] = popupType;

                // 첫 로드는 그대로 사용
                return tempPopup;
            }

            // 2. 풀 확인
            if (_pool.TryGetValue(popupType, out var pool) && pool.Count > 0)
            {
                var pooledObject = pool.Pop();
                pooledObject.SetActive(true);
                return pooledObject.GetComponent<PopupCore>();
            }

            // 3. 새로 로드
            return LoadAsset(key);
        }

        private PopupCore LoadAsset(string key)
        {
            var loadedAsset = AssetManager.Instance.GetAsset<GameObject>(key);
            if (loadedAsset == null)
            {
                DGDebug.LogError<PopupController>($"Load Asset Failed: {key}");
                return null;
            }

            var popupCore = loadedAsset.GetComponent<PopupCore>();
            if (popupCore != null)
            {
                popupCore.Init();
                popupCore.SetActive(false);
            }
            return popupCore;
        }

        private void RemoveFromPool(PopupCore instance)
        {
            var type = instance.GetType();
            if (!_pool.TryGetValue(type, out var pool)) return;

            var temp = new List<GameObject>(pool);
            pool.Clear();
            foreach (var go in temp)
                if (go != instance.gameObject) pool.Push(go);
        }

        private void ReturnToPool(PopupCore popup)
        {
            var popupType = popup.GetType();
            popup.gameObject.SetActive(false);

            if (!_pool.ContainsKey(popupType))
            {
                _pool[popupType] = new Stack<GameObject>();
            }

            _pool[popupType].Push(popup.gameObject);
        }
    }
}