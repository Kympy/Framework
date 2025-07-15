using UnityEngine.UI;

namespace Framework.Extensions
{
    public static class ImageExtensions
    {
        public static void SetSprite(this Image image, string key)
        {
            if (AtlasManager.HasInstance == false)
            {
                throw new System.Exception("AtlasManager not initialized");
            }
            image.sprite = AtlasManager.Instance.GetSprite(key);
        }
    }
}
