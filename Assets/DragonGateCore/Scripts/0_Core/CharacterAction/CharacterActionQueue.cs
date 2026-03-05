using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace DragonGate
{
    public enum CharacterActionEnqueueType
    {
        Default = 0,
        ReplaceIfSameAsCurrent,
        IgnoreIfSameAsCurrent
    }
    
    public class CharacterActionQueue
    {
        public bool IsExecuting => _isExecuting;
        
        private readonly Queue<ICharacterAction> _actionQueue = new();
        private bool _isExecuting;
        private Pawn _owner;
        private ICharacterAction _currentAction;

        public CharacterActionQueue(Pawn owner)
        {
            _owner = owner;
        }
        
        public void EnqueueAction(ICharacterAction newAction, CharacterActionEnqueueType enqueueType = CharacterActionEnqueueType.Default)
        {
            switch (enqueueType)
            {
                case CharacterActionEnqueueType.ReplaceIfSameAsCurrent:
                {
                    if (_currentAction?.GetActionType() == newAction.GetActionType())
                    {
                        _currentAction?.Cancel(_owner);
                    }
                    break;
                }
                case CharacterActionEnqueueType.IgnoreIfSameAsCurrent:
                {
                    if (_currentAction?.GetActionType() == newAction.GetActionType()) return;
                    break;
                }
            }
            _actionQueue.Enqueue(newAction);
            TryProcessQueue();
        }

        private void TryProcessQueue()
        {
            if (_isExecuting == false)
            {
                ProcessQueue().Forget();
            }
        }

        private async UniTaskVoid ProcessQueue()
        {
            _isExecuting = true;

            while (_actionQueue.Count > 0)
            {
                _currentAction = _actionQueue.Dequeue();
                await _currentAction.Execute(_owner);
                // 소유자가 null 이면 큐 진행 자체를 중단.
                if (_owner == null) break;
                _currentAction.OnActionCompleted?.Invoke();
            }
            _isExecuting = false;
            _currentAction = null;
        }

        public void CancelCurrentAction()
        {
            _currentAction?.Cancel(_owner);
            _owner.CancelToken();
        }

        public void ClearQueue()
        {
            _actionQueue.Clear();
        }
    }
}
