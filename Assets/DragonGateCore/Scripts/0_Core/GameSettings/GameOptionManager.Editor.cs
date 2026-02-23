#if UNITY_EDITOR
using UnityEditor;

namespace DragonGate
{
    public partial class GameOptionManager
    {
        [MenuItem("SaveData/Option/Delete")]
        public static void Delete()
        {
            if (ES3.KeyExists(OptionKey) == false)
            {
                DGDebug.Log("Option key is not exists.");
                return;
            }
            ES3.DeleteKey(OptionKey);
            DGDebug.Log("Game Option Delete Success");
        }

        [MenuItem("SaveData/Option/To Default")]
        public static void ToDefault()
        {
            if (ES3.KeyExists(OptionKey) == false)
            {
                DGDebug.Log("Option key is not exists.");
                return;
            }
            var option = ES3.Load<GameOption>(OptionKey);
            option.SetDefault();
            ES3.Save(OptionKey, option);
            DGDebug.Log("Game Option To Default Success");
        }
    }
}
#endif