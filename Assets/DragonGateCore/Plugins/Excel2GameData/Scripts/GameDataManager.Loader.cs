using System;
using System.Collections.Generic;
using System.IO;
using DragonGate;
using UnityEngine;
using UnityEngine.AddressableAssets;

public partial class GameDataManager
{
    public static void Load<T>(string resourceKey, ref Dictionary<long, T> container) where T : IGameData<T>
    {
        LoadInternal(resourceKey, ref container);
    }
    
    private static bool LoadInternal<T>(string resourceKey, ref Dictionary<long, T> container) where T : IGameData<T>
    {
        container.Clear();
#if DEBUG_BUILD
        UnityEngine.Debug.Log($"Start Load GameData : {resourceKey}");
#endif
        var handle = Addressables.LoadAssetAsync<TextAsset>(resourceKey);
#if DEBUG_BUILD
        UnityEngine.Debug.Log($"Start Load GameData : {resourceKey} / IsDone : {handle.IsDone} / Percent : {handle.PercentComplete}");
#endif
        TextAsset text;
        try
        {
            text = handle.WaitForCompletion();
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Table Load Failed : {resourceKey} / {ex}");
            Addressables.Release(handle);
            throw;
        }

        if (text == null)
        {
            Addressables.Release(handle);
            throw new FileLoadException();
        }

        T[] items = FromJson<T>($"{{\"Items\":{text.text}}}");
        int rowCount = items.Length;
        container.Clear();
        for (int i = 0; i < rowCount; i++)
        {
            bool success = container.TryAdd(items[i].LongKey, items[i]);
            if (success == false)
            {
                UnityEngine.Debug.LogError($"Table add error > Id : {items[i].LongKey} / TableName : {resourceKey}");
                Addressables.Release(handle);
                throw new InvalidKeyException();
            }
        }

        Addressables.Release(handle);
#if DEBUG_BUILD
        UnityEngine.Debug.Log($"Table Load Success : {resourceKey}");
#endif
        return true;
    }
    
    [System.Serializable]
    public class Wrapper<T> where T : IGameData<T>
    {
        public T[] Items;
    }

    private static T[] FromJson<T>(string textData) where T : IGameData<T>
    {
        var json = JsonUtility.FromJson<Wrapper<T>>(textData);
        if (json == null)
        {
            UnityEngine.Debug.LogError($"json is null {textData}");
            return null;
        }
        return json.Items;
    }
}
