#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using DragonGate;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

namespace DragonGate
{
    public static class AtlasMapper
    {
        private static string SaveDirectory = "Assets/Resources/Generated";

        [MenuItem("Tools/Sprites/Generate Sprite Map")]
        public static void GenerateSpriteToAtlasMap()
        {
// 중복 체크 및 팝업 경고
            var map = new Dictionary<string, string>();
            var duplicateKeys = new HashSet<string>();
            var guids = AssetDatabase.FindAssets("t:SpriteAtlas");
            
            if (guids.Length == 0)
            {
                UnityEngine.Debug.LogError("[SpriteAtlasMapper] Atlas 없음.");
                EditorUtility.DisplayDialog("스프라이트 맵 생성", "Atlas가 없습니다.", "확인");
                return;
            }

            foreach (var guid in guids)
            {
                var atlasPath = AssetDatabase.GUIDToAssetPath(guid);
                var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
                var tempSprites = new Sprite[atlas.spriteCount];
                atlas.GetSprites(tempSprites);

                foreach (var sprite in tempSprites)
                {
                    string cleanName = sprite.name.Replace("(Clone)", "").Trim();
                    if (map.TryAdd(cleanName, Path.GetFileNameWithoutExtension(atlasPath)) == false)
                    {
                        duplicateKeys.Add(cleanName);
                    }
                }
            }

            if (duplicateKeys.Count > 0)
            {
                string message = $"중복된 스프라이트 이름이 {duplicateKeys.Count}개 발견되었습니다.\n\n" +
                                 string.Join("\n", duplicateKeys);
                UnityEngine.Debug.LogError("[SpriteAtlasMapper] 중복된 스프라이트 이름:\n" + message);
                EditorUtility.DisplayDialog("중복된 스프라이트 이름 발견", message, "확인");
                return;
            }

            var data = new SpriteMappingInfo();
            data.Infos = new SpriteMap[map.Count];
            int index = 0;
            foreach (var pair in map)
            {
                SpriteMap spriteMap = new SpriteMap();
                spriteMap.SpriteName = pair.Key;
                spriteMap.AtlasName = pair.Value;
                data.Infos[index] = spriteMap;
                index++;
            }

            if (Directory.Exists(SaveDirectory) == false)
                Directory.CreateDirectory(SaveDirectory);

            // map을 JSON 저장
            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(Path.Combine(SaveDirectory, "sprite_map.json"), json);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("스프라이트 맵 생성", "성공", "확인");
            UnityEngine.Debug.Log("스프라이트 맵 생성 완료.");
        }
    }
}
#endif