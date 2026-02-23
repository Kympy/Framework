using UnityEngine;

namespace DragonGate
{
    public static class RendererExtensions
    {
        // x,y 평면만 사용하는 환경에서 y좌표를 기준으로 정렬 순서를 정함.
        public static void UpdateSortingOrderByY(this SpriteRenderer spriteRenderer, Transform transform)
        {
            spriteRenderer.sortingOrder = -(int)(transform.position.y * 100);
        }

        // 알파 설정
        public static void SetAlpha(this SpriteRenderer spriteRenderer, float alpha)
        {
            var originColor = spriteRenderer.color;
            spriteRenderer.color = new Color(originColor.r, originColor.g, originColor.b, alpha);
        }
    }
}