using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DragonGate
{
    public class DamageTextManager : Singleton<DamageTextManager>
    {
        private EDamageTextType _damageTextType;
        private DamageTextBlock _instancePrefab;
        private Canvas _parentCanvas;
        private Camera _camera;
        private Vector3 _lastDamagedPosition;

        private const string ResourceKey = "DamageTextBlock";

        public void Initialize(Canvas parentCanvas)
        {
            _parentCanvas = parentCanvas;
            _camera = _parentCanvas.renderMode == RenderMode.ScreenSpaceCamera ? _parentCanvas.worldCamera : Camera.main;
        }
        
        public UniTask Preload()
        {
            if (string.IsNullOrEmpty(ResourceKey))
            {
                DGDebug.LogError<DamageTextManager>("ResourceKey is null or empty");
                return UniTask.CompletedTask;
            }
            return UniTask.CompletedTask;
            // await DGGameObjectPool<DamageTextBlock>.CreateAsync(ResourceKey);
        }

        public void SetDamageTextType(EDamageTextType damageTextType)
        {
            _damageTextType = damageTextType;
        }

        public void ShowDamageText(int damage, Vector3 worldPosition, bool firstDamage = false)
        {
            switch (_damageTextType)
            {
                default:
                case EDamageTextType.Unspecified:
                    DGDebug.LogError<DamageTextManager>("Unspecified damage text type");
                    return;
                case EDamageTextType.Stack:
                    var targetWorldPosition = firstDamage ? worldPosition : _lastDamagedPosition += Vector3.up * 5f;
                    var screenPosition = _camera.WorldToScreenPoint(targetWorldPosition);
                    ShowDamageTextInternal(damage, screenPosition);
                    _lastDamagedPosition = targetWorldPosition;
                    return;
                case EDamageTextType.Pop:
                    return;
            }
        }

        private void ShowDamageTextInternal(int damage, Vector2 screenPosition)
        {
            // var block = DGGameObjectPool<DamageTextBlock>.Get(ResourceKey);
            // block.Show(damage, screenPosition, _damageTextType, _parentCanvas.transform);
        }
    }
}