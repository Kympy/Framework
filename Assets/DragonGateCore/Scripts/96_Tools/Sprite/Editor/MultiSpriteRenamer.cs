using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.U2D.Sprites;

namespace DragonGate
{
    public class MultiSpriteRenamer
    {
        [MenuItem("Assets/Sprite/Multiple Sprite 이름 변경", true)]
        private static bool ValidateRename()
        {
            // Only enable when a texture is selected
            var tex = Selection.activeObject as Texture2D;
            if (tex == null) return false;

            string path = AssetDatabase.GetAssetPath(tex);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            return importer != null && importer.spriteImportMode == SpriteImportMode.Multiple;
        }

        [MenuItem("Assets/Sprite/Multiple Sprite 이름 변경")]
        private static void RenameMultipleSprites()
        {
            var tex = Selection.activeObject as Texture2D;
            if (tex == null) return;

            string path = AssetDatabase.GetAssetPath(tex);
            string baseName = System.IO.Path.GetFileNameWithoutExtension(path);
            
            // 메타 파일 삭제
            string metaPath = path + ".meta";
            if (File.Exists(metaPath))
            {
                File.Delete(metaPath);
                UnityEngine.Debug.Log($"메타파일 삭제됨: {metaPath}");
            }
            AssetDatabase.Refresh();

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || importer.spriteImportMode != SpriteImportMode.Multiple)
            {
                UnityEngine.Debug.LogError("선택한 텍스처는 Multiple Sprite가 아닙니다.");
                return;
            }
            
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            
            var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
            dataProvider.InitSpriteEditorDataProvider();
            
            var metas = dataProvider.GetSpriteRects();
            for (int i = 0; i < metas.Length; i++)
            {
                metas[i].name = $"{baseName}_{i}";
            }
            dataProvider.SetSpriteRects(metas);
            dataProvider.Apply();
            importer.SaveAndReimport();

            UnityEngine.Debug.Log($"'{baseName}' 내 Sprite {metas.Length}개의 이름이 재설정되었습니다.");
        }
        
        [MenuItem("Assets/Sprite/폴더를 대상으로 Sprite 이름 변경", true)]
        private static bool ValidateFolderRename()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return Directory.Exists(path);
        }

        [MenuItem("Assets/Sprite/폴더를 대상으로 Sprite 이름 변경")]
        private static void RenameAllInFolder()
        {
            string folderPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            string[] allFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);

            int totalRenamed = 0;
            foreach (string file in allFiles)
            {
                if (file.EndsWith(".png") || file.EndsWith(".jpg") || file.EndsWith(".tga"))
                {
                    if (RenameAtPath(file))
                        totalRenamed++;
                }
            }

            AssetDatabase.Refresh();
            UnityEngine.Debug.Log($"총 {totalRenamed}개의 텍스처에 대해 Sprite 리네임 완료됨.");
        }

        private static bool RenameAtPath(string path)
        {
            string unityPath = path.Replace(Application.dataPath, "Assets");

            var importer = AssetImporter.GetAtPath(unityPath) as TextureImporter;
            if (importer == null || importer.spriteImportMode != SpriteImportMode.Multiple)
                return false;

            string baseName = Path.GetFileNameWithoutExtension(unityPath);

            // 메타 파일 삭제
            string metaPath = path + ".meta";
            if (File.Exists(metaPath))
            {
                File.Delete(metaPath);
                UnityEngine.Debug.Log($"메타파일 삭제됨: {metaPath}");
            }

            AssetDatabase.Refresh();
            
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            
            var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
            dataProvider.InitSpriteEditorDataProvider();
            
            var spriteRects = dataProvider.GetSpriteRects();
            for (int i = 0; i < spriteRects.Length; i++)
            {
                spriteRects[i].name = $"{baseName}_{i}";
            }
            dataProvider.SetSpriteRects(spriteRects);
            dataProvider.Apply();
            importer.SaveAndReimport();
            
            UnityEngine.Debug.Log($"'{baseName}' 내 Sprite {spriteRects.Length}개의 이름이 재설정되었습니다.");
            return true;
        }
    }

}