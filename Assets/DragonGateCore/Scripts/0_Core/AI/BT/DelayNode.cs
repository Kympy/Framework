using System;
using UnityEngine;

namespace DragonGate
{
    [Serializable]
    // [BTNode("Delay", "Utility")]
    [BTNode("Delay")]
    [BTCategory("Utility")]
    public class DelayNode : BTNode
    {
        [SerializeField] public float duration = 1f;

        private float _startTime;
        private bool _started;

        public DelayNode() { }
        public DelayNode(float duration) => this.duration = duration;

        public override Result Tick()
        {
            if (!_started)
            {
                _startTime = Time.time;
                _started = true;
            }

            if (Time.time - _startTime >= duration)
            {
                _started = false;
                return Result.Success;
            }

            return Result.Running;
        }
    }
}