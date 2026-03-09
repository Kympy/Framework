using UnityEngine;
using UnityEngine.Localization.Settings;

namespace DragonGate
{
    public partial class LocalizationUtil
    {
        public static string GetRandomKey(string tableCollectionName)
        {
            var table = LocalizationSettings.StringDatabase.GetTable(tableCollectionName);

            if (table == null || table.Count == 0)
            {
                DGDebug.LogError($"StringTable not found or empty: {tableCollectionName}");
                return null;
            }

            int randomIndex = Random.Range(0, table.Count);

            int index = 0;
            foreach (var entry in table)
            {
                if (index == randomIndex)
                    return entry.Value.Key;

                index++;
            }

            return null;
        }
    }
}
