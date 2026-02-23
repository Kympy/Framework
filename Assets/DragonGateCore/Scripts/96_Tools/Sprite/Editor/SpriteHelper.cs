using UnityEditor;
using UnityEngine;

namespace DragonGate
{
    public static partial class SpriteHelper
    {
        [MenuItem("Assets/Sprite/Single Sprite로 변경")]
        public static void SwitchToSingleSprite()
        {
            Object[] selectedObjects = Selection.objects;

            if (selectedObjects == null || selectedObjects.Length == 0)
            {
                UnityEngine.Debug.LogWarning("선택된 에셋이 없다.");
                return;
            }

            foreach (Object selectedObject in selectedObjects)
            {
                string assetPath = AssetDatabase.GetAssetPath(selectedObject);

                if (string.IsNullOrEmpty(assetPath))
                    continue;

                TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;

                if (textureImporter == null)
                    continue;

                // Texture 타입을 Sprite로 설정
                textureImporter.textureType = TextureImporterType.Sprite;

                // Single 모드로 설정
                textureImporter.spriteImportMode = SpriteImportMode.Single;

                textureImporter.SaveAndReimport();
            }

            UnityEngine.Debug.Log("Single Sprite 변경 완료");
        }
    }
}
