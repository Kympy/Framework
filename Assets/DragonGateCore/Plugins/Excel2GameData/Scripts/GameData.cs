using DragonGate;

public interface IGameData<T>
{
    public long LongKey { get; }
}

[System.Serializable]
public class GameData<T> : IGameData<T> where T : GameData<T>
{
    public string KEY;
    public long LongKey => KEY.ToHash();
}

