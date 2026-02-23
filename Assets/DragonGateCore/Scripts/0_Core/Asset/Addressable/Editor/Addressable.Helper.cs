using UnityEditor;

namespace DragonGate
{
    public static partial class AddressableHelper
    {
        [MenuItem("Tools/Addressables/Open Addressables Groups Window")]
        public static void OpenAddressablesWindow()
        {
            EditorApplication.ExecuteMenuItem("Window/Asset Management/Addressables/Groups");
        }
    }
}
