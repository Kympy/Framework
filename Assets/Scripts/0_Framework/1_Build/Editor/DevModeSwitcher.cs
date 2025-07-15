using UnityEditor;

namespace Framework
{
    public class DevModeSwitcher
    {
        private const string debug_Define = "DEBUG_BUILD";
        private const string release_Define = "RELEASE_BUILD";
    
        [MenuItem("Build/Switch Mode/Debug")]
        public static void SetDebugMode()
        {
            DefineSymbolManager.RemoveSymbol(release_Define);
            DefineSymbolManager.AddSymbol(debug_Define);
            
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
        }

        [MenuItem("Build/Switch Mode/Release")]
        public static void SetReleaseMode()
        {
            DefineSymbolManager.RemoveSymbol(debug_Define);
            DefineSymbolManager.AddSymbol(release_Define);
            
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
        }
    }
}
