using UnityEngine;
using UnityEngine.UI;

namespace Framework
{
    public class BetterHorizontalLayoutGroup : HorizontalLayoutGroup
    {
        public float _yDistance = 0;

        public override void SetLayoutVertical()
        {
            base.SetLayoutVertical();

            // yStep이 0이면 커스텀 적용 안 함
            if (Mathf.Approximately(_yDistance, 0f))
                return;

            // Y 위치 변경을 여기서 해줌
            for (int i = 0; i < rectChildren.Count; i++)
            {
                RectTransform child = rectChildren[i];
                if (child == null) continue;

                Vector3 pos = child.anchoredPosition;
                pos.y = pos.y + i * _yDistance; // 요소마다 Y 증가
                child.anchoredPosition = pos;
            }
        }
    }
}