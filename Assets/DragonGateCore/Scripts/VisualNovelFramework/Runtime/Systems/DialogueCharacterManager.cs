using System.Collections.Generic;
using UnityEngine;

namespace DragonGate
{
    public class DialogueCharacterManager
    {
        private readonly Dictionary<int, DialogueCharacter> _characters = new();

        private DialogueCharacter GetCharacter(DialogueCharacterAsset asset)
        {
            if (_characters.TryGetValue(asset.Id, out DialogueCharacter character) == false)
            {
                character = PoolManager.Instance.GetComponent<DialogueCharacter>(asset.CharacterPrefab.RuntimeKey.ToString());
                _characters.Add(asset.Id, character);
            }
            
            return character;
        }

        public void ShowCharacter(DialogueCharacterAsset asset, Vector2 position, float scale = 1)
        {
            var character = GetCharacter(asset);
            var worldPosition = CameraManager.CurrentCamera.ViewportToWorldPoint(position);
            worldPosition.z = 0;
            character.transform.position = worldPosition;
            character.transform.localScale = new Vector3(scale, scale, scale);
        }

        public void HideCharacter(int characterId)
        {
            if (_characters.TryGetValue(characterId, out DialogueCharacter character) == false) return;
            PoolManager.Instance.ReturnComponent(character);
            _characters.Remove(characterId);
        }

        public void HideAllCharacter()
        {
            foreach (var character in _characters.Values)
            {
                PoolManager.Instance.ReturnComponent(character);
            }
            _characters.Clear();
        }

        public void PlayAnimation(int characterId, string triggerName)
        {
            if (_characters.TryGetValue(characterId, out DialogueCharacter character) == false)
            {
                return;
            }
            character.SetAnimationTrigger(triggerName);
        }
    }
}
