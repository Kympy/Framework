using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

namespace Framework.Editor
{
    public class AtlasMapper
    {
        [MenuItem("Tools/Generate Sprite Map")]
        public static void GenerateSpriteToAtlasMap()
        {
#if UNITY_EDITOR
            var map = new Dictionary<string, string>();
            var guids = AssetDatabase.FindAssets("t:SpriteAtlas");
            foreach (var guid in guids)
            {
                var atlasPath = AssetDatabase.GUIDToAssetPath(guid);
                var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
                // 아틀라스 내부 모든 스프라이트 검색
                var allAssets = AssetDatabase.LoadAllAssetsAtPath(atlasPath);
                var sprites = new List<Sprite>();
                foreach (var asset in allAssets)
                {
                    if (asset is Sprite sprite)
                    {
                        sprites.Add(sprite);
                    }
                }

                foreach (var sprite in sprites)
                {
                    if (!map.ContainsKey(sprite.name))
                    {
                        map[sprite.name] = Path.GetFileNameWithoutExtension(atlasPath);
                    }
                }
            }

            var data = new SpriteMappingInfo();
            data.Infos = new SpriteMap[map.Count];
            int index = 0;
            foreach (var pair in map)
            {
                data.Infos[index].SpriteName = pair.Key;
                data.Infos[index].AtlasName = pair.Value;
                index++;
            }

            // map을 JSON 저장
            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText("Assets/Resources/Generated/sprite_map.json", json);
            AssetDatabase.Refresh();
#endif
        }
    }
}
