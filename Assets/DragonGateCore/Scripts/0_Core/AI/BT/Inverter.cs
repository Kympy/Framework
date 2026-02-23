using System;

namespace DragonGate
{
    [Serializable]
    // [BTNode("Inverter", "Decorator")]
    [BTNode("Inverter")]
    [BTCategory("Decorator")]
    public class Inverter : DecoratorNode
    {
        public Inverter() { }
        public Inverter(BTNode child) : base(child) { }

        public override Result Tick()
        {
            var result = _child.Tick();
            if (result == Result.Success) return Result.Failure;
            if (result == Result.Failure) return Result.Success;
            return Result.Running;
        }
    }
}