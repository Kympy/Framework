using UnityEngine;
using UnityEditor;
using UnityEditor.U2D.Sprites;

namespace DragonGate
{
    public class SpritePivotEditor
    {
        private static Vector2 _middleCenterPivot = new Vector2(0.5f, 0.5f);
        private static Vector2 _bottomCenterPivot = new Vector2(0.5f, 0f);
        
        [MenuItem("Tools/Sprites/Set Pivot to Bottom Center (Select Folder)")]
        private static void SetPivotBottomCenter()
        {
            SetPivot(_bottomCenterPivot);
        }
        
        [MenuItem("Tools/Sprites/Set Pivot to Middle Center (Select Folder)")]
        private static void SetPivotMiddleCenter()
        {
            SetPivot(_middleCenterPivot);
        }

        private static void SetPivot(Vector2 pivot)
        {
            string folderPath = EditorUtility.OpenFolderPanel("Select Sprite Folder", "Assets", "");

            // 경로 없거나 Assets 외부인 경우 무시
            if (string.IsNullOrEmpty(folderPath) || !folderPath.StartsWith(Application.dataPath))
            {
                UnityEngine.Debug.LogWarning("유효한 Assets 폴더를 선택하세요.");
                return;
            }

            // 상대 경로로 변환
            string relativePath = "Assets" + folderPath.Substring(Application.dataPath.Length);

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { relativePath });
            int modifiedCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (importer == null || importer.textureType != TextureImporterType.Sprite)
                    continue;
                
                var factory = new SpriteDataProviderFactories();
                factory.Init();
                
                var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
                dataProvider.InitSpriteEditorDataProvider();
                
                if (importer.spriteImportMode == SpriteImportMode.Single)
                {
                    importer.spritePivot = pivot;
                }
                else if (importer.spriteImportMode == SpriteImportMode.Multiple)
                {
                    var metas = dataProvider.GetSpriteRects();
                    for (int i = 0; i < metas.Length; i++)
                    {
                        metas[i].alignment = SpriteAlignment.Custom;
                        metas[i].pivot = new Vector2(0.5f, 0f);
                    }
                    dataProvider.SetSpriteRects(metas);
                    dataProvider.Apply();
                }

                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                modifiedCount++;
            }

            AssetDatabase.Refresh();
            UnityEngine.Debug.Log($"완료: {modifiedCount}개의 Sprite Pivot이 Bottom Center로 설정되었습니다.");
        }
    }
}
