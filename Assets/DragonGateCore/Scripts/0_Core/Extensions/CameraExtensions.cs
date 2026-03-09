using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace DragonGate
{
    public static class CameraExtensions
    {
        // 투디에서 스프라이트(아마 가장 큰 배경) 기준으로 Ortho를 맞춤
        public static void FitCameraToSpriteRenderer(this Camera targetCamera, SpriteRenderer spriteRenderer)
        {
            if (targetCamera == null) return;
            if (spriteRenderer == null) return;

            float spriteWorldHeight = spriteRenderer.bounds.size.y;
            targetCamera.orthographicSize = spriteWorldHeight * 0.5f;
        }
        
        public static void FitCameraToSprite(this Camera targetCamera, Sprite targetSprite)
        {
            if (targetCamera == null) return;
            if (targetSprite == null) return;

            float spritePixelHeight = targetSprite.rect.height;
            float pixelsPerUnit = targetSprite.pixelsPerUnit;

            float orthographicSize = spritePixelHeight / (2f * pixelsPerUnit);

            targetCamera.orthographicSize = orthographicSize;
        } 

#if DOTWEEN && UNITASK_DOTWEEN_SUPPORT
        public static async UniTask DOZoom(this Camera camera, ICancelable cancelable, float zoomValue, float duration, bool ignoreTimeScale = true)
        {
            if (camera.orthographic == false)
            {
                var zoomTween = camera.DOFieldOfView(zoomValue, duration).SetEase(Ease.Linear).SetUpdate(ignoreTimeScale);
                await UniTaskHelper.WaitTween(cancelable, zoomTween);
            }
            else
            {
                var zoomTween = camera.DOOrthoSize(zoomValue, duration).SetEase(Ease.Linear).SetUpdate(ignoreTimeScale);
                await UniTaskHelper.WaitTween(cancelable, zoomTween);
            }
        }

        public static async UniTask DOZoomPosition(this Camera camera, ICancelable cancelable, float zoomValue, Vector3 position, float duration, bool ignoreTimeScale = true)
        {
            Tween zoomTween;
            Tween moveTween = camera.transform.DOMove(position, duration)
                .SetEase(Ease.Linear)
                .SetUpdate(ignoreTimeScale);

            if (camera.orthographic == false)
            {
                zoomTween = camera.DOFieldOfView(zoomValue, duration)
                    .SetEase(Ease.Linear)
                    .SetUpdate(ignoreTimeScale);
            }
            else
            {
                zoomTween = camera.DOOrthoSize(zoomValue, duration)
                    .SetEase(Ease.Linear)
                    .SetUpdate(ignoreTimeScale);
            }

            // 줌 + 이동을 동시에 실행
            var sequence = DOTween.Sequence()
                .Join(moveTween)
                .Join(zoomTween);

            await UniTaskHelper.WaitTween(cancelable, sequence);
        }
#endif

        /// <summary>
        /// Shake this camera using the central CameraShaker manager (per-camera rest position is preserved).
        /// </summary>
        public static Tween Shake(this Camera target, float duration, Vector3 strength, bool ignoreTimeScale = false)
        {
            if (target == null) return null;
            if (CameraShaker.HasInstance == false)
            {
                DGDebug.LogError("Camera Shaker.HasInstance == false");
                return null;
            }
            return CameraShaker.Instance.Shake(target, duration, strength, ignoreTimeScale);
        }

        /// <summary>
        /// Shake this camera with uniform (x=y) strength using the central CameraShaker manager.
        /// </summary>
        public static void Shake(this Camera target, float duration, float strength, bool ignoreTimeScale = false)
        {
            if (target == null) return;
            if (CameraShaker.HasInstance == false) return;
            CameraShaker.Instance.Shake(target, duration, strength, ignoreTimeScale);
        }

        /// <summary>
        /// Register this camera to the CameraShaker (optional; Shake auto-registers if needed).
        /// </summary>
        public static void RegisterToShaker(this Camera target)
        {
            if (target == null) return;
            if (CameraShaker.HasInstance == false) return;
            CameraShaker.Instance.AddCamera(target);
        }

        /// <summary>
        /// Unregister this camera from the CameraShaker and restore its rest position.
        /// </summary>
        public static void UnregisterFromShaker(this Camera target)
        {
            if (target == null) return;
            if (CameraShaker.HasInstance == false) return;
            CameraShaker.Instance.RemoveCamera(target);
        }
    }
}
