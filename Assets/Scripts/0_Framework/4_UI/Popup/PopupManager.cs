using System.Collections.Generic;
using UnityEngine;

namespace Framework
{
    public class PopupManager : MonoBehaviourSingleton<PopupManager>
    {
        [SerializeField] protected PopupContainer _popupContainerResource;
        
        protected const int MaxContainer = 10;
        
        protected Stack<PopupContainer> _popupContainerPool = new Stack<PopupContainer>();
        protected Stack<PopupContainer> _activePopupStack = new Stack<PopupContainer>();

        protected int _defaultSortOrder = PopupOrder.Default;
        
        public T ShowPopup<T>(string key, IPopupParameter parameter = null) where T : PopupBase
        {
            T popup = CreatePopup<T>(key);

            var container = GetPopupContainer();
            container.SetPopup(popup);
            container.SetOrder(GetNewSortOrder());
            popup.BeforeShow(parameter);
            popup.gameObject.SetActive(true);
            _activePopupStack.Push(container);
            return popup;
        }
        
        public T ShowPopup<T>(string key, PopupOption option, IPopupParameter parameter = null) where T : PopupBase
        {
            T popup = CreatePopup<T>(key);

            var container = GetPopupContainer();
            container.SetPopup(popup);
            container.SetOrder(GetNewSortOrder());
            container.SetOption(option);
            popup.BeforeShow(parameter);
            popup.gameObject.SetActive(true);
            _activePopupStack.Push(container);
            return popup;
        }

        public void ClosePopup()
        {
            if (_activePopupStack.Count == 0) return;
            var container = _activePopupStack.Pop();
            container.SetPopup(null);
            ReturnContainer(container);
        }

        public int GetNewSortOrder()
        {
            return _defaultSortOrder + _activePopupStack.Count;
        }
        
        private T CreatePopup<T>(string key) where T : PopupBase
        {
            var popupObject = AssetManager.Instance.GetAsset<T>(UIAsset.GetKey<T>());
            popupObject.gameObject.SetActive(false);
            return popupObject;
        }
        
        private void CloseAllPopups()
        {
            while (_activePopupStack.Count > 0)
            {
                ClosePopup();
            }
        }
        
        protected PopupContainer GetPopupContainer()
        {
            if (_popupContainerPool.Count > 0)
            {
                return _popupContainerPool.Pop();
            }
            var newContainer = Object.Instantiate(_popupContainerResource);
            newContainer.gameObject.SetActive(false);
            return newContainer;
        }

        protected void ReturnContainer(PopupContainer container)
        {
            if (_popupContainerPool.Count >= MaxContainer)
            {
                Object.Destroy(container.gameObject);
                return;
            }
            _popupContainerPool.Push(container);
        }
    }
}