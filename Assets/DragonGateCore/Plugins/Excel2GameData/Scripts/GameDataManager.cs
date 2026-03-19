
public sealed partial class GameDataManager
{
    public static GameDataManager Instance { get; private set; } = null;
#if UNITY_EDITOR
    public static GameDataManager Editor
    {
        get
        {
            CreateInstance();
            return Instance;
        }
    }
#endif
    
    public static void CreateInstance()
    {
        if (Instance != null) return;
        Instance = new GameDataManager();
    }
    
    private GameDataManager() { }
}
