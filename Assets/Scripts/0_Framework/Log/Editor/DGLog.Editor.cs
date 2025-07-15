using UnityEditor;

namespace Framework
{
    public partial class DGLog
    {
#if UNITY_EDITOR
        [MenuItem("Tools/DGLog/Enable")]
        public static void EnableLog()
        {
            DefineSymbolManager.AddSymbol("DGLOG");
        }

        [MenuItem("Tools/DGLog/Disable")]
        public static void DisableLog()
        {
            DefineSymbolManager.RemoveSymbol("DGLOG");
        }
#endif
    }
}
