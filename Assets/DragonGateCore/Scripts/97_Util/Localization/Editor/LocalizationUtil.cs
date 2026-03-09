using UnityEditor;
using UnityEditor.Localization;

namespace DragonGate.Editor
{
    public partial class LocalizationUtil
    {
        [MenuItem("Localization/Tables")]
        public static void OpenStringTableCollection()
        {
            EditorApplication.ExecuteMenuItem("Window/Asset Management/Localization Tables");
        }
    }
}
