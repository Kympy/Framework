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
        
        [IntChoice(typeof(UISortOrder))]
        [Header("Sort"), SerializeField] protected int _defaultSortOrder = UISortOrder.PopupDefault;
        [Space]
        // 다중 팝업 허용 여부 : 기본적으로 False
        [Header("Settings")]
        public bool AllowMultipleInstance = false;
        public bool BlockBackgroundInput = true;
        public EPopupBackgroundType BackgroundType = EPopupBackgroundType.Black;
        
        public void SetContainer(PopupContainer container)
        {
            Container = container;
        }
    }
}