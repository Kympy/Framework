using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DragonGate
{
    public class DialoguePreviewSettings : ScriptableObject
    {
        public AssetReference DialogueRunnerPrefab;
        [Space]
        public float DefaultTextSpeed = 0.05f;
        public Vector2 DefaultCharacterViewportPosition = new Vector2(.5f, .5f);
        public float DefaultCharacterScale = 1f;
    }
}