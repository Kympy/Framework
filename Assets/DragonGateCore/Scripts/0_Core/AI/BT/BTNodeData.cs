using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonGate
{
    /// <summary>
    /// BTGraphAsset 안에 저장되는 노드 하나의 직렬화 데이터.
    /// </summary>
    [Serializable]
    public class BTNodeData
    {
        public bool IsRoot;
        // 노드 고유 식별자 (에디터에서 GUID로 생성)
        public string guid;

        // 런타임에서 GetNode<T>(key)로 찾을 때 사용하는 이름 (선택)
        public string nodeKey;
        
        // 노드 종류에 대한 카테고리 이름.
        public string CategoryName;

        // BTNode 서브클래스의 AssemblyQualifiedName
        public string typeName;

        // 에디터에서의 위치
        public Vector2 position;

        // 자식 노드들의 guid (순서 보존)
        public List<string> childrenGuids = new();

        // JSON으로 직렬화한 노드 파라미터 (public 또는 [SerializeField] 필드)
        public string paramJson;
    }
}