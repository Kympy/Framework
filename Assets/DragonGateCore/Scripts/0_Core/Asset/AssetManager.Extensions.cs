using UnityEngine;
using UnityEngine.UI;

namespace DragonGate
{
    public partial class AssetManager { }
    
    public static class AssetManagerExtensions
    {
        // 이 메서드를 통해 스프라이트를 세팅해야, 자동으로 릴리즈 됨.
        public static void SetSprite(this Image image, object key)
        {
            var autoRelease = image.GetOrAddComponent<SpriteAutoRelease>();
            
            if (key == null)
            {
                autoRelease.RemoveSpriteReference();
                image.sprite = null;
                return;
            }
            
            var sprite = AssetManager.Instance.GetAsset<Sprite>(new AssetKey(key));
            autoRelease.SetSpriteReference(sprite);

            image.sprite = sprite;
        }

        public static void SetSprite(this SpriteRenderer spriteRenderer, object key)
        {
            var autoRelease = spriteRenderer.GetOrAddComponent<SpriteAutoRelease>();

            if (key == null)
            {
                autoRelease.RemoveSpriteReference();
                spriteRenderer.sprite = null;
                return;
            }
            var sprite = AssetManager.Instance.GetAsset<Sprite>(new AssetKey(key));
            autoRelease.SetSpriteReference(sprite);
            
            spriteRenderer.sprite = sprite;
        }
    }
}
