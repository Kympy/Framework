using DragonGate;
using UnityEditor;

namespace DragonGate
{
    public class EditorToolMenu
    {
        [MenuItem("Tools/Refresh Data, Addressables", false, priority: 0)]
        public static void OneClickRefresh()
        {
            Excel2GameDataWindow.OnOneClick();
            AddressableHelper.SetAddressables();
        }
    }
}