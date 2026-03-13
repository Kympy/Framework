using System;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;

namespace DragonGate
{
    public interface ICharacterAction
    {
        public bool IsCanceled { get; }
        public Action OnActionCompleted { get; }
        public Type GetActionType();
        public AssetReferenceSprite GetActionIcon();
        public UniTask Execute(Pawn pawn);
        public void Cancel(Pawn pawn);
    }
    
    // 나중에 풀링해야 할듯.
    public sealed class CharacterAction<TParameter> : ICharacterAction
    {
        public bool IsCanceled { get; private set; }
        public CharacterActionDefinition<TParameter> ActionDefinition { get; private set; }
        public TParameter Parameter { get; private set; }
        public Action OnActionCompleted { get; private set; }

        public CharacterAction(CharacterActionDefinition<TParameter> actionDefinition, TParameter parameter, Action onActionCompleted = null)
        {
            ActionDefinition = actionDefinition;
            Parameter = parameter;
            OnActionCompleted = onActionCompleted;
            IsCanceled = false;
        }

        public Type GetActionType()
        {
            return ActionDefinition.GetType();
        }

        public UniTask Execute(Pawn pawn)
        {
            return ActionDefinition.Execute(pawn, Parameter);
        }

        public void Cancel(Pawn pawn)
        {
            IsCanceled = true; // 중복 캔슬을 방지하고자 플래그
            ActionDefinition.Cancel(pawn, Parameter);
        }

        public AssetReferenceSprite GetActionIcon()
        {
            return ActionDefinition.ActionIcon;
        }
    }
}
