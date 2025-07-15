using UnityEngine;
using UnityEngine.UI;

namespace Framework
{
    public class BetterVerticalLayoutGroup : VerticalLayoutGroup
    {
        [SerializeField] private float _xDistance = 0;

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
                pos.x = pos.x + i * _xDistance; // 요소마다 Y 증가
                child.anchoredPosition = pos;
            }
        }
    }
}