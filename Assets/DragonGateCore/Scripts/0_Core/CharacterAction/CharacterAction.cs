using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;

namespace DragonGate
{
    public interface ICharacterAction
    {
        public bool Cancelable { get; }
        public bool IsCanceled { get; }
        public float ProgressNormalized { get; }
        public Action OnActionCompleted { get; }
        public string GetActionKey();
        public string SerializeParameter();
        public AssetReferenceSprite GetActionIcon();
        public UniTask Execute(Pawn pawn);
        public void RequestCancel();       // CTS만 취소 → Execute가 OperationCanceledException 던짐
        public UniTask Cancel(Pawn pawn);  // 정리 작업 (StandUp 등) → ProcessQueue에서만 호출
    }

    public sealed class CharacterAction<TParameter> : ICharacterAction
    {
        public bool Cancelable => ActionDefinition.Cancelable;
        public bool IsCanceled { get; private set; }
        public float ProgressNormalized { get; private set; }
        public CharacterActionDefinition<TParameter> ActionDefinition { get; private set; }
        public TParameter Parameter { get; private set; }
        public Action OnActionCompleted { get; private set; }

        private readonly float _initialProgress;
        private CancellationTokenSource _executionCts;

        public CharacterAction(CharacterActionDefinition<TParameter> actionDefinition, TParameter parameter, Action onActionCompleted = null, float initialProgress = 0f)
        {
            ActionDefinition = actionDefinition;
            Parameter = parameter;
            OnActionCompleted = onActionCompleted;
            IsCanceled = false;
            ProgressNormalized = initialProgress;
            _initialProgress = initialProgress;
        }

        public string GetActionKey() => ActionDefinition.GetActionKey();

        public string SerializeParameter() => ActionDefinition.SerializeParameter(Parameter);

        public UniTask Execute(Pawn pawn)
        {
            _executionCts = UniTaskHelper.CreateObjectToken(pawn);
            var progressReporter = new Progress<float>(value => ProgressNormalized = value);
            return ActionDefinition.Execute(pawn, Parameter, _executionCts.Token, _initialProgress, progressReporter);
        }

        public void RequestCancel()
        {
            IsCanceled = true;
            _executionCts?.Cancel();
            _executionCts?.Dispose();
            _executionCts = null;
        }

        public UniTask Cancel(Pawn pawn)
        {
            return ActionDefinition.Cancel(pawn, Parameter);
        }

        public AssetReferenceSprite GetActionIcon()
        {
            return ActionDefinition.ActionIcon;
        }
    }
}
