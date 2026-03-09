using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DragonGate
{
    public struct DialogueSnapshotData
    {
        public string BackgroundSpriteKey;
        public string BgmKey;
        public float BgmVolume;
        public List<CharacterSnapshotData> Characters;
    }

    public struct CharacterSnapshotData
    {
        public AssetReferenceT<DialogueCharacterAsset> CharacterAsset;
        public Vector2 Position;
        public float Scale;
    }
}
