namespace DragonGate
{
    public abstract class DecoratorNode : BTNode
    {
        protected BTNode _child;

        protected DecoratorNode() { }
        public DecoratorNode(BTNode child) => _child = child;

        public void SetChild(BTNode child) => _child = child;

        public override void SetBlackboard(Blackboard bb)
        {
            base.SetBlackboard(bb);
            _child?.SetBlackboard(bb);
        }
    }
}