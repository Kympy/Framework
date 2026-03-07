using System.Collections.Generic;
using UnityEngine;

namespace DragonGate
{
    public class SpriteAutoRelease : AssetReleaseHelper
    {
        private Sprite _currentSprite;

        public void SetSpriteReference(Sprite sprite)
        {
            _currentSprite = sprite;
        }

        public void RemoveSpriteReference()
        {
            Release();
            _currentSprite = null;
        }

        protected override void Release()
        {
            base.Release();
            
            if (_currentSprite != null && AssetManager.HasInstance)
                AssetManager.Instance.ReleaseAsset(_currentSprite);
        }
    }
}
