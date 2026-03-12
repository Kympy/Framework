using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DragonGate
{
    public class DialoguePreviewSettings : ScriptableObject
    {
        [Header("대화 재생 프리팹")]
        public AssetReference DialogueRunnerPrefab;
        [Space, Header("텍스트 설정")]
        public float DefaultTextSpeed = 0.05f;
        public float DefaultTextSize = 30f;
        public Color DefaultTextColor = Color.white;
        [Space, Header("캐릭터 설정")]
        public Vector2 DefaultCharacterViewportPosition = new Vector2(.5f, .5f);
        public float DefaultCharacterScale = 1f;
        [Space, Header("Color")]
        public Color DefaultStartColor = Color.white;
        public Color DefaultEndColor = Color.white;
    }
}