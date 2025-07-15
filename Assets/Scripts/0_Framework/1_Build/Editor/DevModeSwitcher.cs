using UnityEditor;

namespace Framework
{
    public static class DevModeSwitcher
    {
        public const string DebugDefine = "DEBUG_BUILD";
    
        [MenuItem("Build/Switch Mode/Debug")]
        public static void SetDebugMode()
        {
            DefineSymbolManager.AddSymbol(DebugDefine);
            
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
        }

        [MenuItem("Build/Switch Mode/Release")]
        public static void SetReleaseMode()
        {
            DefineSymbolManager.RemoveSymbol(DebugDefine);
            
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
        }
    }
}
