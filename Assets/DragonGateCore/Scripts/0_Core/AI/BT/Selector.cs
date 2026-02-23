using System;
using System.Collections.Generic;

namespace DragonGate
{
    [Serializable]
    [BTNode("Selector")]
    [BTCategory("Composite")]
    public class Selector : CompositeNode
    {
        public Selector() { }
        public Selector(List<BTNode> children) : base(children) { }
        // 자식이 하나라도 성공, running이면 종료
        public override Result Tick()
        {
            foreach (var child in _children)
            {
                var result = child.Tick();
                if (result != Result.Failure)
                    return result;
            }
            return Result.Failure;
        }
    }
}