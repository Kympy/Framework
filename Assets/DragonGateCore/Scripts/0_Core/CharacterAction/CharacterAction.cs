using System;
using Cysharp.Threading.Tasks;

namespace DragonGate
{
    public interface ICharacterAction
    {
        public Action OnActionCompleted { get; }
        public Type GetActionType();
        public UniTask Execute(Pawn pawn);
        public void Cancel(Pawn pawn);
    }
    
    public sealed class CharacterAction<TParameter> : ICharacterAction
    {
        public CharacterActionDefinition<TParameter> ActionDefinition { get; private set; }
        public TParameter Parameter { get; private set; }
        public Action OnActionCompleted { get; private set; }

        public CharacterAction(CharacterActionDefinition<TParameter> actionDefinition, TParameter parameter, Action onActionCompleted = null)
        {
            ActionDefinition = actionDefinition;
            Parameter = parameter;
            OnActionCompleted = onActionCompleted;
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
            ActionDefinition.Cancel(pawn);
        }
    }
}
