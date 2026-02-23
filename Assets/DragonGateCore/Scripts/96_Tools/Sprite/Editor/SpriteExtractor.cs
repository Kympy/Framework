using UnityEditor;
using UnityEngine;
using System.IO;

namespace DragonGate
{
    public static class SpriteExtractor
    {
        [MenuItem("Assets/Sprite/Multiple Sprite 를 단일 PNG로 분리")]
        public static void SplitMultipleSprite()
        {
            Texture2D texture = Selection.activeObject as Texture2D;
            if (texture == null)
            {
                UnityEngine.Debug.LogError("선택된 텍스처가 없습니다.");
                return;
            }

            string path = AssetDatabase.GetAssetPath(texture);
            string dir = Path.GetDirectoryName(path);
            string baseName = Path.GetFileNameWithoutExtension(path);

            Object[] sprites = AssetDatabase.LoadAllAssetsAtPath(path);
            int count = 0;

            foreach (var obj in sprites)
            {
                if (obj is Sprite sprite)
                {
                    // Sprite의 영역 가져오기
                    Rect rect = sprite.rect;
                    Texture2D tex = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.ARGB32, false);
                    Color[] pixels = texture.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
                    tex.SetPixels(pixels);
                    tex.Apply();

                    // PNG로 저장
                    byte[] png = tex.EncodeToPNG();
                    string fileName = $"{baseName}_{count:D2}.png";
                    File.WriteAllBytes(Path.Combine(dir, fileName), png);

                    count++;
                    Object.DestroyImmediate(tex);
                }
            }

            AssetDatabase.Refresh();
            UnityEngine.Debug.Log($"✅ {count}개의 PNG로 분리 저장 완료");
        }
        
        [MenuItem("Assets/Sprite/Single Sprite를 PNG로 저장")]
        public static void ExtractPngFromSprite()
        {
            var sprite = Selection.activeObject as Sprite;
            if (sprite == null) return;

            Texture2D tex = sprite.texture;
            Rect rect = sprite.textureRect;
            var pixels = tex.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);

            Texture2D newTex = new Texture2D((int)rect.width, (int)rect.height);
            newTex.SetPixels(pixels);
            newTex.Apply();

            byte[] png = newTex.EncodeToPNG();
            string path = Application.dataPath + $"/{sprite.name}_extracted.png";
            File.WriteAllBytes(path, png);
            AssetDatabase.Refresh();
        }
    }

}