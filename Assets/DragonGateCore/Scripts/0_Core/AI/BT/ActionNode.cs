namespace DragonGate
{
    public abstract class ActionNode : BTNode
    {
        private bool _hasEntered;

        public sealed override Result Tick()
        {
            if (!_hasEntered)
            {
                _hasEntered = true;
                OnEnter();
            }
            var result = OnTick();
            if (result != Result.Running)
            {
                _hasEntered = false;
            }
            return result;
        }

        protected abstract Result OnTick();
    }
}