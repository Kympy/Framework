using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DragonGate
{
    public class DialogueCharacterManager
    {
        private DialogueRunner _runner;
        
        public DialogueCharacterManager(DialogueRunner runner)
        {
            _runner = runner;
        }
        
        private struct CharacterData
        {
            public AssetReferenceT<DialogueCharacterAsset> AssetRef;
            public Vector2 ViewportPosition;
            public float Scale;
            public DialogueCharacter Character;
        }

        private readonly Dictionary<AssetKey, CharacterData> _characters = new();
        private readonly Dictionary<AssetKey, PoolHandle<DialogueCharacter>> _characterPools = new();

        private AssetKey GetKey(AssetReferenceT<DialogueCharacterAsset> assetRef) => new AssetKey(assetRef.RuntimeKey);

        public void ShowCharacter(AssetReferenceT<DialogueCharacterAsset> assetRef, Vector2 position, float scale = 1)
        {
            var key = GetKey(assetRef);
            DialogueCharacter character;
            if (_characters.TryGetValue(key, out CharacterData existing))
            {
                character = existing.Character;
            }
            else
            {
                var characterAsset = AssetManager.Instance.GetAsset<DialogueCharacterAsset>(key);
                var characterKey = new AssetKey(characterAsset.CharacterPrefab.RuntimeKey);
                if (_characterPools.TryGetValue(characterKey, out PoolHandle<DialogueCharacter> pool) == false)
                {
                    pool = PoolScope.CreatePool<DialogueCharacter>(PoolScopeLoader.FromFunc(() => AssetManager.Instance.GetAsset<GameObject>(characterKey)));
                    _characterPools.Add(characterKey, pool);
                }
                character = pool.Get();
            }
            var worldPosition = CameraManager.CurrentCamera.ViewportToWorldPoint(position);
            worldPosition.z = 0;
            character.TeleportTo(worldPosition);
            character.transform.localScale = new Vector3(scale, scale, scale);
            _characters[key] = new CharacterData { AssetRef = assetRef, ViewportPosition = position, Scale = scale, Character = character };
        }

        public async UniTask MoveCharacter(AssetReferenceT<DialogueCharacterAsset> assetRef, Vector2 position, Ease easeType = Ease.Linear, float duration = 0.5f, float scale = 1f)
        {
            var key = GetKey(assetRef);
            if (_characters.TryGetValue(key, out CharacterData existing) == false)
            {
                DGDebug.LogError($"Move Character - Not exists character: {key}");
                return;
            }
            var worldPosition = CameraManager.CurrentCamera.ViewportToWorldPoint(position);
            worldPosition.z = 0;
            var tween = existing.Character.MoveTo(worldPosition, easeType, duration, scale);
            await UniTaskHelper.WaitTween(_runner, tween);
        }

        public void TeleportCharacter(AssetReferenceT<DialogueCharacterAsset> assetRef, Vector2 position, float scale = 1f)
        {
            var key = GetKey(assetRef);
            if (_characters.TryGetValue(key, out CharacterData existing) == false)
            {
                DGDebug.LogError($"Move Character - Not exists character: {key}");
                return;
            }
            var worldPosition = CameraManager.CurrentCamera.ViewportToWorldPoint(position);
            worldPosition.z = 0;
            existing.Character.TeleportTo(worldPosition);
            existing.Character.transform.localScale = new Vector3(scale, scale, scale);
        }

        public void Invert(AssetReferenceT<DialogueCharacterAsset> assetRef, bool inverted)
        {
            var key = GetKey(assetRef);
            if (_characters.TryGetValue(key, out CharacterData existing) == false)
            {
                DGDebug.LogError($"Invert Character - Not exists character: {key}");
                return;
            }
            existing.Character.Invert(inverted);
        }

        public async UniTask ColorFade(AssetReferenceT<DialogueCharacterAsset> assetRef, Color start, Color end, float duration)
        {
            var key = GetKey(assetRef);
            if (_characters.TryGetValue(key, out CharacterData existing) == false)
            {
                DGDebug.LogError($"Fade Character - Not exists character: {key}");
                return;
            }

            if (duration <= 0f)
            {
                existing.Character.SetColor(end);
                return;
            }
            var tween = existing.Character.FadeColor(start, end, duration);
            await UniTaskHelper.WaitTween(_runner, tween);
        }

        public async UniTask ToTransparent(AssetReferenceT<DialogueCharacterAsset> assetRef, float duration)
        {
            var key = GetKey(assetRef);
            if (_characters.TryGetValue(key, out CharacterData existing) == false)
            {
                DGDebug.LogError($"Fade Character - Not exists character: {key}");
                return;
            }

            if (duration <= 0f)
            {
                existing.Character.SetColor(Color.clear);
                return;
            }
            var tween = existing.Character.ToTransparent(duration);
            await UniTaskHelper.WaitTween(_runner, tween);
        }

        public async UniTask ToVisible(AssetReferenceT<DialogueCharacterAsset> assetRef, float duration)
        {
            var key = GetKey(assetRef);
            if (_characters.TryGetValue(key, out CharacterData existing) == false)
            {
                DGDebug.LogError($"Fade Character - Not exists character: {key}");
                return;
            }

            if (duration <= 0f)
            {
                existing.Character.SetColor(Color.clear);
                return;
            }
            var tween = existing.Character.ToVisible(duration);
            await UniTaskHelper.WaitTween(_runner, tween);
        }

        public void SetScale(AssetReferenceT<DialogueCharacterAsset> assetRef, float scale)
        {
            var key = GetKey(assetRef);
            if (_characters.TryGetValue(key, out CharacterData existing) == false)
            {
                DGDebug.LogError($"Move Character - Not exists character: {key}");
                return;
            }
            existing.Character.transform.localScale = new Vector3(scale, scale, scale);
        }

        public List<CharacterSnapshotData> GetSnapshots()
        {
            var list = new List<CharacterSnapshotData>(_characters.Count);
            foreach (var data in _characters.Values)
            {
                list.Add(new CharacterSnapshotData
                {
                    CharacterAsset = data.AssetRef,
                    Position = data.ViewportPosition,
                    Scale = data.Scale,
                });
            }
            return list;
        }

        public void HideCharacter(AssetReferenceT<DialogueCharacterAsset> assetRef)
        {
            var key = GetKey(assetRef);
            if (_characters.TryGetValue(key, out CharacterData data) == false)
            {
                DGDebug.LogError($"Hide Character - Not exists : {key}");
                return;
            }
            PoolScope.Return(data.Character);
            _characters.Remove(key);
        }

        public void HideAllCharacter()
        {
            foreach (var data in _characters.Values)
            {
                PoolScope.Return(data.Character);
            }
            _characters.Clear();
        }

        public void PlayAnimation(AssetReferenceT<DialogueCharacterAsset> assetRef, string triggerName)
        {
            var key = GetKey(assetRef);
            if (_characters.TryGetValue(key, out CharacterData data) == false)
            {
                DGDebug.LogError($"Play Animation - Not exists : {key}");
                return;
            }
            data.Character.SetAnimationTrigger(triggerName);
        }

        public void ShakeCharacter(AssetReferenceT<DialogueCharacterAsset> assetRef, Vector2 strength, float duration)
        {
            var key = GetKey(assetRef);
            if (_characters.TryGetValue(key, out CharacterData data) == false)
            {
                DGDebug.LogError($"Shake Character - Not exists : {key}");
                return;
            }
            data.Character.transform.DOShakePosition(duration, strength);
        }
    }
}