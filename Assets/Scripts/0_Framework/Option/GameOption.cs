using UnityEngine;

namespace Framework
{
    public class GameOption : Singleton<GameOption>
    {
        public int FrameRate { get; private set; } = 60;
        
        public void SetFrameRate(int frameRate)
        {
            FrameRate = frameRate;
            Application.targetFrameRate = frameRate;
        }
    }
}
