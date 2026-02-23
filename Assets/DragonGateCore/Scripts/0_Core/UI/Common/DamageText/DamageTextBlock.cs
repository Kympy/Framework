using Cysharp.Threading.Tasks;
using System.Collections;
using DragonGate;
using TMPro;
using UnityEngine;

namespace DragonGate
{
    public class DamageTextBlock : UICore
    {
        // [SerializeField] protected Graphics[] _graphics;
        [SerializeField] protected TextMeshProUGUI _damageText;
        [Space]
        [SerializeField] protected EDamageTextType _damageTextType;
        [SerializeField] protected float _duration = 1;

        public virtual void Show(int damage, Vector2 screenPosition, EDamageTextType type = EDamageTextType.Unspecified, Transform parent = null)
        {
            if (parent != null && parent != transform.parent)
            {
                transform.SetParent(parent);
            }
            _damageText.alpha = 0;
            _damageText.SetInt(damage);
            _damageText.transform.position = screenPosition;
            var targetType = type == EDamageTextType.Unspecified ? _damageTextType : type;
            Animate(targetType);
        }

        protected void Animate(EDamageTextType type)
        {
            switch (type)
            {
                case EDamageTextType.Static:
                {
                    StaticAnimation().Forget();
                    break;
                }
                case EDamageTextType.Stack:
                {
                    break;
                }
            }
        }

        protected async UniTask StaticAnimation()
        {
            _damageText.alpha = 1;
            await UniTaskHelper.WaitForSeconds(this, _duration);
            _damageText.alpha = 0;
        }

        protected UniTask StackAnimation()
        {
            _damageText.alpha = 1;
            return UniTask.CompletedTask;
#if DOTWEEN
            // transform.DO
#endif
        }
    }
}