using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;

namespace DragonGate
{
    [Serializable]
    [CreateAssetMenu (menuName = "Visual Novel/Dialogue Character Asset", fileName = "New Dialogue Character Asset")]
    public class DialogueCharacterAsset : ScriptableObject
    {
        [SerializeField] private int _id;
        [SerializeField] private LocalizedString _characterName;
        [SerializeField] private AssetReference _characterPrefab;

        public int Id => _id;
        public LocalizedString Name => _characterName;
        public AssetReference CharacterPrefab => _characterPrefab;
        
        public bool IsValidCharacterAsset => _characterPrefab != null && _characterPrefab.RuntimeKeyIsValid();

        [CanBeNull]
        public string GetName()
        {
            if (_characterName == null || _characterName.IsEmpty) return null;
            return _characterName.GetLocalizedString();
        }
    }
}
