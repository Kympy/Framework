using UnityEngine;

namespace DragonGate
{
    public class GameOption
    {
        public long LastPlayTime; // 마지막 접속 시간
        
        public int FrameRate = 60;
        public bool CameraShake = true;
        public float MouseSensitivity = 20f;
        public float BgmVolume = 1;
        public float SfxVolume = 1;
        public SystemLanguage Language;

        public void SetDefault()
        {
            Language = Application.systemLanguage;
            FrameRate = 60;
            CameraShake = true;
            MouseSensitivity = 20f;
            BgmVolume = 1;
            SfxVolume = 1;
        }

        public override string ToString()
        {
            return "FrameRate:" + FrameRate;
        }
    }
    
    public partial class GameOptionManager : Singleton<GameOptionManager>
    {
        public static GameOption Values { get; private set; }
        public const string OptionKey = "GameOption";
        
        public static SystemLanguage CurrentLanguage => Values == null ? Application.systemLanguage : Values.Language;
        
        public delegate void OnLanguageChangedDelegate(SystemLanguage language);
        public OnLanguageChangedDelegate OnLanguageChanged;

        // public override void Init()
        // {
        //     base.Init();
        //     LoadOption();
        // }

        public void SetFrameRate(int frameRate)
        {
            Values.FrameRate = frameRate;
            Application.targetFrameRate = frameRate;
        }

        public void SaveOption()
        {
            ES3.Save(OptionKey, Values);
        }

        // TODO : ES3 덜어내기
        public void LoadOption()
        {
            if (ES3.KeyExists(OptionKey))
            {
                Values = ES3.Load<GameOption>(OptionKey);
            }
            else
            {
                Values = new GameOption();
                Values.SetDefault();
            }
            DGDebug.Log("Game Option Loaded.");
        }

        public void ChangeLanguage(SystemLanguage language)
        {
            Values.Language = language;
            OnLanguageChanged?.Invoke(language);
        }
    }
}
