using System;
using System.Collections.Generic;

namespace DragonGate
{
    [Serializable]
    [BTNode("Sequence")]
    [BTCategory("Composite")]
    public class Sequence : CompositeNode
    {
        private int _current;

        public Sequence() { }
        public Sequence(List<BTNode> children) : base(children) { }
        // 자식이 모두 성공해야 종료, 하나라도 실패하면 실패 반환.
        public override Result Tick()
        {
            while (_current < _children.Count)
            {
                var result = _children[_current].Tick();
                switch (result)
                {
                    case Result.Running:
                        return Result.Running;
                    case Result.Failure:
                        _current = 0;
                        return Result.Failure;
                    case Result.Success:
                        _current++;
                        break;
                }
            }

            _current = 0;
            return Result.Success;
        }
    }
}