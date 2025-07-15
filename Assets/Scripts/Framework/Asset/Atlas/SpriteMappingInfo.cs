using System.Collections.Generic;

namespace Framework
{
    public class RuntimeSpriteMappingInfo
    {
        private Dictionary<string, string> _spriteMappingInfo;

        public RuntimeSpriteMappingInfo(SpriteMappingInfo mappingInfo)
        {
            _spriteMappingInfo = new Dictionary<string, string>();
            for (int i = 0; i < mappingInfo.Infos.Length; i++)
            {
                var map = mappingInfo.Infos[i];
                if (_spriteMappingInfo.TryAdd(map.SpriteName, map.AtlasName) == false)
                {
                    throw new System.Exception($"SpriteMappingInfo already contains the key: {map.SpriteName}");
                }
            }
        }

        public string GetOwnedAtlasName(string spriteName)
        {
            return _spriteMappingInfo.GetValueOrDefault(spriteName, null);
        }
    }
    
    [System.Serializable]
    public class SpriteMappingInfo
    {
        public SpriteMap[] Infos;
    }

    [System.Serializable]
    public struct SpriteMap
    {
        public string SpriteName;
        public string AtlasName;
    }
}
