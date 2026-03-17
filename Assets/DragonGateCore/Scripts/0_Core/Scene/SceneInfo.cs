using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DragonGate
{
    [CreateAssetMenu (menuName = "Scene/Create Scene Info")]
    [System.Serializable]
    public class SceneInfo : ScriptableObject
    {
        public AssetReference SceneReference;
        public AssetReferenceGameObject LoadingScreenReference;
        public SceneInfo NextSceneInfo;
    }
}
