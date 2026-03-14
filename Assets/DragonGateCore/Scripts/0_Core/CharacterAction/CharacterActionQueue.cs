using System;
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
    
    // 큐지만 사실 LinkedList임. -> 왜냐 중간요소를 제거할 일이 있기 때문
    public class CharacterActionQueue
    {
        public bool IsExecuting => _isExecuting;
        public int Count => _actionQueue.Count;
        
        private readonly LinkedList<ICharacterAction> _actionQueue = new();
        private bool _isExecuting;
        private Pawn _owner;
        private ICharacterAction _currentAction;
        
        public event Action<ICharacterAction> OnActionAdded;
        public event Action<ICharacterAction> OnActionRemoved;
        public event Action<ICharacterAction> OnActionCanceled;

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
                        OnActionCanceled?.Invoke(_currentAction);
                    }
                    break;
                }
                case CharacterActionEnqueueType.IgnoreIfSameAsCurrent:
                {
                    if (_currentAction?.GetActionType() == newAction.GetActionType()) return;
                    break;
                }
            }
            _actionQueue.AddLast(newAction);
            OnActionAdded?.Invoke(newAction);
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
                _currentAction = _actionQueue.First.Value;
                await _currentAction.Execute(_owner);
                // 행동 다 하고 빼야한다.
                _actionQueue.Remove(_currentAction);
                OnActionRemoved?.Invoke(_currentAction);
                // 소유자가 null 이면 큐 진행 자체를 중단.
                if (_owner == null) break;
                _currentAction.OnActionCompleted?.Invoke();
            }
            _isExecuting = false;
            _currentAction = null;
        }

        public void CancelAll()
        {
            ClearQueue();
            _currentAction?.Cancel(_owner);
        }

        public void CancelCurrentAction()
        {
            // 현재 액션이면 캔슬에 대한 요청만 하고 종료 (왜냐면 캔슬은 즉시 없애는 것이 아니라, 액션의 종료 상태로 빠르게 보내는 것이기 때문)
            _currentAction?.Cancel(_owner);
            OnActionCanceled?.Invoke(_currentAction);
        }

        public void CancelAction(int targetIndex)
        {
            var current = GetActionNodeAt(targetIndex);

            if (current == null) return;

            if (_currentAction == current.Value)
            {
                // 이미 캔슬 중인 액션은 리턴
                if (_currentAction.IsCanceled) return;
                CancelCurrentAction();
                return;
            }
            _actionQueue.Remove(current);
            OnActionRemoved?.Invoke(current.Value);
        }

        public void ClearQueue()
        {
            _actionQueue.Clear();
        }

        public LinkedListNode<ICharacterAction> GetActionNodeAt(int targetIndex)
        {
            var current = _actionQueue.First;
            var index = 0;
            while (current != null)
            {
                if (index == targetIndex)
                    break;
                current = current.Next;
                index++;
            }
            return current;
        }
    }
}
