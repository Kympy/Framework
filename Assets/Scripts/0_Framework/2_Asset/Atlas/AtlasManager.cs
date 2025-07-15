using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

namespace Framework
{
    public class AtlasManager : Singleton<AtlasManager>
    {
        private RuntimeSpriteMappingInfo _spriteMappingInfo;
        private Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

        public AtlasManager()
        {
            var asset = Resources.Load<TextAsset>("Generated/sprite_map");
            if (asset != null)
            {
                var mappingInfo = JsonUtility.FromJson<SpriteMappingInfo>(asset.text);
                _spriteMappingInfo = new RuntimeSpriteMappingInfo(mappingInfo);
            }
            Resources.UnloadAsset(asset);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        public Sprite GetSprite(string key)
        {
            if (_spriteCache.TryGetValue(key, out var sprite))
            {
                return sprite;
            }

            var atlasName = _spriteMappingInfo.GetOwnedAtlasName(key);
            var atlas = AssetManager.Instance.GetAsset<SpriteAtlas>(atlasName);
            if (atlas == null) return null;
            sprite = atlas.GetSprite(key);
            if (sprite == null) return null;

            _spriteCache.TryAdd(key, sprite);
            return sprite;
        }
        
        public void ClearSpriteCache()
        {
            foreach (var cache in _spriteCache)
            {
                Object.Destroy(cache.Value);
            }
            _spriteCache.Clear();
        }
    }
}
