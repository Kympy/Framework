using System;

namespace DragonGate
{
    /// <summary>
    /// BT 비주얼 에디터에 노드를 등록하는 어트리뷰트.
    /// BTNode 서브클래스에 붙이면 에디터 컨텍스트 메뉴에 나타난다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class BTNodeAttribute : Attribute
    {
        public string DisplayName { get; }

        public BTNodeAttribute(string displayName)
        {
            DisplayName = displayName;
        }
    }
}