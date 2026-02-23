using System.Collections.Generic;
using UnityEngine;

namespace DragonGate
{
    /// <summary>
    /// 블랙보드 키 정의 에셋. 여러 BT 그래프가 공유하거나 개별 할당 가능.
    /// Generate Keys 버튼으로 BBKey string 상수 파일을 생성한다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBlackboard", menuName = "AI/Blackboard")]
    public class BlackboardAsset : ScriptableObject
    {
        public List<BlackboardKeyDefinition> keys = new();

        // 코드 생성 설정
        public string generatedKeysClassName;  // 예: StaffBBKeys
        public string generatedKeysFilePath;   // 예: Assets/Scripts/AI/StaffBBKeys.cs
    }
}