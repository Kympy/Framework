using System.Collections.Generic;
using UnityEngine;

namespace DragonGate
{
    /// <summary>
    /// BT 비주얼 에디터의 작업 파일. 에디터 전용.
    /// 런타임에서는 사용되지 않는다 — Generate Code로 C# 파일을 생성해서 사용.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBrain", menuName = "AI/BT Graph")]
    public class BTGraphAsset : ScriptableObject
    {
        // 이 BT가 사용할 블랙보드 에셋
        public BlackboardAsset blackboardAsset;

        // 그래프 데이터
        public string rootGuid;
        public List<BTNodeData> nodes = new();

        // 코드 생성 설정
        public string targetClassName;   // 생성할 partial 클래스 이름  (예: xxxBrain)
        public string generatedFilePath; // 출력 경로 (예: Assets/Scripts/AI/xxxBrain.BT.Generated.cs)
    }
}