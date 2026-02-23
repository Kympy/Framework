using System;
using UnityEngine;

namespace DragonGate
{
    public abstract class BTBrain : MonoBehaviour
    {
        [SerializeField] protected BlackboardAsset _blackboardAsset;
        protected BTRunner _runner;

        public void Init(MonoBehaviour self)
        {
            _runner = self.GetOrAddComponent<BTRunner>();
            _runner.InitTree(BuildTreeInCode(), CreateBlackboard(self));
        }

        public void StartAI()
        {
            _runner.StartTree();
        }

        /// <summary>
        /// BT 트리를 조립해서 반환한다.
        /// 코드 생성 방식: 에디터의 Generate Code로 partial 클래스에 자동 구현된다.
        /// </summary>
        protected virtual BTNode BuildTreeInCode()
        {
            DGDebug.LogWarning($"[{GetType().Name}] BuildTreeInCode가 구현되지 않았습니다. BT Graph Editor에서 Generate Code를 실행하세요.");
            return new Sequence();
        }

        protected virtual Blackboard CreateBlackboard(object self)
        {
            var bb = new Blackboard();
            bb.Initialize(_blackboardAsset, self);
            return bb;
        }

        public void StopAI() => _runner?.StopTree();
    }
}