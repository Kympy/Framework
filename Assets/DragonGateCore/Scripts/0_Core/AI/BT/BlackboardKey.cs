using System.Collections.Generic;

namespace DragonGate
{
    /// <summary>
    /// 타입이 각인된 블랙보드 키. Blackboard.GetKey&lt;T&gt;()로만 생성한다.
    /// Store 참조를 캐싱하므로 Get/Set 시 타입 딕셔너리 조회가 제거된다.
    /// </summary>
    public readonly struct BlackboardKey<T>
    {
        public readonly int Id;
        internal readonly Dictionary<int, T> Store;

        internal BlackboardKey(int id, Dictionary<int, T> store)
        {
            Id    = id;
            Store = store;
        }
    }
}