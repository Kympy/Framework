public abstract class GameInstanceSubsystem
{
    public abstract void InitSubsystem();
    public abstract void StartSubsystem();
    public abstract void StopSubsystem();
    public abstract void DestroySubsystem();
}

public abstract class GameInstanceSubsystem<T> : GameInstanceSubsystem where T : GameInstanceSubsystem<T>, new()
{
    public static T Instance
    {
        get
        {
            _instance ??= CreateInstance();
            return _instance;
        }
    }
    
    private static T _instance;
    
    private static T CreateInstance()
    {
        if (_instance != null)
        {
            throw new System.Exception($"An instance of this subsystem {typeof(T)} already exists. Use Instance property to access it.");
        }
        _instance ??= new T();
        _instance.InitSubsystem();
        return _instance;
    }
}
