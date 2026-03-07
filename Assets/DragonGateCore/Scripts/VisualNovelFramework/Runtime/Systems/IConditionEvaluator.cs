using System.Collections.Generic;

namespace DragonGate
{
    /// <summary>
    /// 컨디션 노드의 실제 평가 로직을 외부에서 구현하기 위한 인터페이스.
    /// 프레임워크 외부(게임 콘텐츠 레이어)에서 이 인터페이스를 구현하여
    /// DialogueRunner.ConditionEvaluator 에 등록하면 됩니다.
    /// </summary>
    public interface IConditionEvaluator
    {
        /// <summary>
        /// 주어진 조건 타입을 평가하여 true/false 를 반환합니다.
        /// </summary>
        /// <returns>조건들의 AND 연산이 참이면 true, 거짓이면 false</returns>
        bool Evaluate(List<DialogueCondition> conditions);
    }
}