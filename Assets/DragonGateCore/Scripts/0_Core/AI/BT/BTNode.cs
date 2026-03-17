namespace DragonGate
{
    public abstract class BTNode
    {
        protected Blackboard _blackboard;

        // 에셋에서 빌드된 후 코드에서 특정 노드를 찾을 때 사용하는 식별자
        public string NodeKey { get; set; }

        public enum Result { Success, Failure, Running }
        
        public virtual void OnEnter() { }
        public abstract Result Tick();

        public virtual void SetBlackboard(Blackboard bb)
        {
            _blackboard = bb;
            OnBlackboardSet();
        }

        /// <summary>
        /// SetBlackboard 이후 호출. GetKey로 키를 resolve해 필드에 캐싱하는 용도.
        /// </summary>
        protected virtual void OnBlackboardSet() { }
    }
}