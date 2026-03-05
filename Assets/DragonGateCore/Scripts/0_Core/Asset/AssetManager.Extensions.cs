using UnityEngine;
using UnityEngine.UI;

namespace DragonGate
{
    public partial class AssetManager { }
    
    public static class AssetManagerExtensions
    {
        public static void SetSprite(this Image image, string key)
        {
            image.sprite = AssetManager.Instance.GetAsset<Sprite>(key);
        }

        public static void SetSprite(this SpriteRenderer spriteRenderer, string key)
        {
            spriteRenderer.sprite = AssetManager.Instance.GetAsset<Sprite>(key);
        }
    }
}
