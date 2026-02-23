using System.Collections.Generic;

namespace DragonGate
{
    public abstract class CompositeNode : BTNode
    {
        protected List<BTNode> _children = new();

        public CompositeNode(List<BTNode> children)
        {
            _children = children;
        }

        protected CompositeNode()
        {
            
        }

        public void AddChild(BTNode node) => _children.Add(node);

        public override void SetBlackboard(Blackboard bb)
        {
            base.SetBlackboard(bb);
            for (int i = 0; i < _children.Count; i++)
            {
                _children[i].SetBlackboard(bb);
            }
        }
    }
}