public interface ISetGameData<T> where T : GameData<T>
{
    public void SetData(in T data);
}