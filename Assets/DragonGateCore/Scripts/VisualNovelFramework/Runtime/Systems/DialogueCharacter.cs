using UnityEngine;

namespace DragonGate
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class DialogueCharacter : CoreBehaviour
    {
        [SerializeField] protected Animator _animator;

        protected SpriteRenderer _spriteRenderer;
        
        public void SetAnimationTrigger(string trigger)
        {
            _animator.SetTrigger(trigger);
        }
    }
}
