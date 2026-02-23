using System;

namespace DragonGate
{
    // 노드의 카테고리를 정의하는 Attribute. 상속가능함.
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class BTCategoryAttribute : Attribute
    {
        public string Category { get; }

        public BTCategoryAttribute(string category)
        {
            Category = category;
        }
    }
}
