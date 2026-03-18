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
        public int Count => _actionQueue.Count;
        
        private readonly LinkedList<ICharacterAction> _actionQueue = new();
        private bool _isExecuting;
        private Pawn _owner;
        private ICharacterAction _currentAction;
        
        public ICharacterAction CurrentAction => _currentAction;

        public event Action<ICharacterAction> OnActionAdded;
        public event Action<ICharacterAction> OnActionRemoved;
        public event Action<ICharacterAction> OnActionCanceled;
        public event Action OnQueueCompleted; // 큐가 완전히 비었을 때 (정상 완료 시)

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
                    if (_currentAction?.GetActionKey() == newAction.GetActionKey())
                    {
                        CancelCurrentAction();
                    }
                    break;
                }
                case CharacterActionEnqueueType.IgnoreIfSameAsCurrent:
                {
                    if (_currentAction?.GetActionKey() == newAction.GetActionKey()) return;
                    break;
                }
            }
            _actionQueue.AddLast(newAction);
            OnActionAdded?.Invoke(newAction);
            if (_isExecuting == false)
            {
                _isExecuting = true;
                ProcessQueue().Forget();
            }
        }

        private async UniTaskVoid ProcessQueue()
        {
            while (_actionQueue.Count > 0)
            {
                var action = _actionQueue.First.Value;
                _currentAction = action;
                try
                {
                    await action.Execute(_owner);
                }
                catch (System.OperationCanceledException)
                {
                    // Execute가 중단됨 → Cancel로 정리 작업 수행
                    if (action.Cancelable)
                    {
                        await action.Cancel(_owner);
                        OnActionCanceled?.Invoke(action);
                    }
                }
                _actionQueue.Remove(action);
                OnActionRemoved?.Invoke(action);
                _currentAction = null;
                // 소유자가 없거나 비활성이면 큐 중단.
                if (_owner == null || _owner.isActiveAndEnabled == false) break;
                if (action.IsCanceled == false)
                    action.OnActionCompleted?.Invoke();
            }
            _isExecuting = false;
            _currentAction = null;
            OnQueueCompleted?.Invoke();
        }

        public void ClearAll()
        {
            ClearQueue();
            CancelCurrentAction();
        }

        // flag만 세움 → ProcessQueue가 OperationCanceledException을 잡아서 Cancel 처리
        public void CancelCurrentAction()
        {
            if (_currentAction == null) return;
            if (_currentAction.Cancelable == false) return;
            if (_currentAction.IsCanceled) return;

            _currentAction.RequestCancel();
        }

        public void CancelAction(int targetIndex)
        {
            var current = GetActionNodeAt(targetIndex);

            if (current == null) return;

            if (_currentAction == current.Value)
            {
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

        // 현재 실행 중인 액션 + 대기 중인 액션 전부 순회 (저장용)
        public System.Collections.Generic.IEnumerable<ICharacterAction> GetAllActions()
        {
            if (_currentAction != null)
                yield return _currentAction;
            var node = _actionQueue.First;
            while (node != null)
            {
                if (node.Value != _currentAction)
                    yield return node.Value;
                node = node.Next;
            }
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
