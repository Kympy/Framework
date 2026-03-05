using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.UI;

namespace DragonGate
{
    public partial class ToolTipManager : Singleton<ToolTipManager>
    {
        private Vector3[] _corners = new Vector3[4];
        private const string ToolTipPrefabKey = "UIToolTip";

        public T GetToolTip<T>(RectTransform rectTransform, string resourceKey) where T : UIToolTip
        {
            var toolTip = CreateToolTip<T>(resourceKey);
            InitToolTip(toolTip, rectTransform);
            return toolTip;
        }

        public UIToolTip GetToolTip(Transform transform, string message) => GetToolTip(transform as RectTransform, message);

        public UIToolTip GetToolTip(RectTransform rectTransform, string message)
        {
            var toolTip = CreateToolTip().SetMessage(message);
            InitToolTip(toolTip, rectTransform);
            return toolTip;
        }

        public UIToolTip GetToolTip(RectTransform rectTransform, TableEntryReference entryReference)
        {
            var toolTip = CreateToolTip().SetMessage(entryReference);
            InitToolTip(toolTip, rectTransform);
            return toolTip;
        }

        public UIToolTip GetToolTip(RectTransform rectTransform, LocalizedString localizedString)
        {
            var toolTip = CreateToolTip().SetMessage(localizedString);
            InitToolTip(toolTip, rectTransform);
            return toolTip;
        }

        private void InitToolTip(UIToolTip toolTip, RectTransform target)
        {
            var tip = toolTip.RectTransform;
            tip.SetParent(target, false);

            // 텍스트가 설정된 뒤 레이아웃을 빌드해야 실제 크기를 얻을 수 있음
            LayoutRebuilder.ForceRebuildLayoutImmediate(tip);

            PlaceToolTip(tip, target);
            toolTip.SetActive(false);
        }

        // 대상 위(기본) 또는 아래에 배치하고, 화면 밖이면 좌우 보정
        private void PlaceToolTip(RectTransform tip, RectTransform target)
        {
            // anchoredPosition 계산을 단순하게 하기 위해 pivot/anchor를 중앙으로 고정
            tip.anchorMin = tip.anchorMax = new Vector2(0.5f, 0.5f);
            tip.pivot = new Vector2(0.5f, 0.5f);

            float targetHalfH = target.rect.height * 0.5f;
            float tipHalfH = tip.rect.height * 0.5f;

            // 기본: 대상 위에 배치
            Vector2 pos = new Vector2(0f, targetHalfH + tipHalfH);
            tip.anchoredPosition = pos;

            if (tip.root.TryGetComponent(out Canvas canvas) == false)
            {
                return;
            }
            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            float scale = canvas.scaleFactor;
            // 우선 tip의 부모는 보통 작은 Ui 단위 요소이기 때문에 Canvas를 Parent로 찾을수없음. 일단 Overlay로 가정.
            // Camera cam = null;
            // float scale = 1;

            // 상단 벗어나는지 체크
            tip.GetWorldCorners(_corners); // 0=BL, 1=TL, 2=TR, 3=BR

            float topY = Mathf.Max(
                RectTransformUtility.WorldToScreenPoint(cam, _corners[1]).y,
                RectTransformUtility.WorldToScreenPoint(cam, _corners[2]).y);

            if (topY > Screen.height)
            {
                // 아래로 이동
                pos.y = -(targetHalfH + tipHalfH);
                tip.anchoredPosition = pos;
                tip.GetWorldCorners(_corners);
            }

            // 좌우 보정
            float minX = float.MaxValue, maxX = float.MinValue;
            foreach (var c in _corners)
            {
                float sx = RectTransformUtility.WorldToScreenPoint(cam, c).x;
                if (sx < minX) minX = sx;
                if (sx > maxX) maxX = sx;
            }

            if (maxX > Screen.width)
                pos.x -= (maxX - Screen.width) / scale;
            else if (minX < 0f)
                pos.x += -minX / scale;

            tip.anchoredPosition = pos;
        }

        public void HideToolTip(UIToolTip toolTip)
        {
            if (toolTip == null) return;
            PoolManager.Instance?.ReturnComponent(toolTip);
        }

        private UIToolTip CreateToolTip()
        {
            return PoolManager.Instance.GetComponent<UIToolTip>(ToolTipPrefabKey);
        }

        private T CreateToolTip<T>(string resourceKey) where T : UIToolTip
        {
            return PoolManager.Instance.GetComponent<T>(resourceKey);
        }
    }
}