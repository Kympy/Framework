using UnityEngine;

namespace DragonGate
{
    public enum EPopupBackgroundType
    {
        Clear,
        Black,
        White,
    }
    
    public class PopupCore : UICore
    {
        // 기본 Sorting Order 를 가지고 이 위로 +1 씩 스택
        public int DefaultSortOrder => _defaultSortOrder;
        public PopupContainer Container { get; private set; }
        
        // 다중 팝업 허용 여부 : 기본적으로 False
        public bool AllowMultipleInstance = false;
        public EPopupBackgroundType BackgroundType = EPopupBackgroundType.Clear;
        public bool BlockBackgroundInput = true;  
        
        [IntChoice(typeof(UISortOrder))]
        protected int _defaultSortOrder = UISortOrder.Default;
        
        public void SetContainer(PopupContainer container)
        {
            Container = container;
        }
    }
}