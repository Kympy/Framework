using UnityEngine;

public abstract class WorldSubsystem : MonoBehaviour
{
    public abstract void InitSubsystem();
    public abstract void StartSubsystem();
    public abstract void StopSubsystem();
}

public abstract class WorldSubsystem<T> : WorldSubsystem where T : WorldSubsystem<T>
{
    
}