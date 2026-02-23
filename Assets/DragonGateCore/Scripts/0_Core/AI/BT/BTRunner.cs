using UnityEngine;

namespace DragonGate
{
    /// <summary>
    /// BTNode 트리를 매 프레임 Tick하는 실행기.
    /// BTBrain.StartAI()에서 AddComponent로 붙이고 StartTree()로 시작한다.
    /// </summary>
    public class BTRunner : MonoBehaviour
    {
        private BTNode _root;
        private Blackboard _blackboard;
        private bool _isRunning;

        public bool IsRunning => _isRunning;

        public void InitTree(BTNode root, Blackboard blackboard = null)
        {
            _root = root;
            _blackboard = blackboard ?? new Blackboard();
            _root?.SetBlackboard(_blackboard);
        }

        public void StartTree()
        {
            _isRunning = true;
        }

        public void StopTree()
        {
            _isRunning = false;
        }

        private void Update()
        {
            if (!_isRunning || _root == null) return;
            _root.Tick();
        }
    }
}