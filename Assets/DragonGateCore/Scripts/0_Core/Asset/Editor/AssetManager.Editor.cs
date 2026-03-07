#if UNITY_EDITOR
namespace DragonGate
{
    public partial class AssetManager
    {
        public static string GetAssetNameFromGUID(string guid)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            return System.IO.Path.GetFileNameWithoutExtension(assetPath);
        }
    }
}
#endif
