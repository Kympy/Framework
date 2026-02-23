using UnityEngine;
using UnityEngine.UI;

namespace DragonGate
{
    public class BetterVerticalLayoutGroup : VerticalLayoutGroup
    {
        [SerializeField] private float _xDistance = 0;
        [SerializeField] private bool _reverse = false;

        public override void SetLayoutHorizontal()
        {
            base.SetLayoutHorizontal();

            // yStep이 0이면 커스텀 적용 안 함
            if (Mathf.Approximately(_xDistance, 0f))
                return;

            // Y 위치 변경을 여기서 해줌
            for (int i = 0; i < rectChildren.Count; i++)
            {
                RectTransform child = rectChildren[i];
                if (child == null) continue;

                Vector3 pos = child.anchoredPosition;
                int index = _reverse ? (rectChildren.Count - 1 - i) : i;
                pos.x = pos.x + index * _xDistance;
                child.anchoredPosition = pos;
            }
        }
    }
}