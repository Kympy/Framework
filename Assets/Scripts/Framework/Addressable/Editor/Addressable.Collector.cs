using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Framework
{
    public static partial class AddressableHelper
    {
        private const string LocalRoot = "Assets/Content/Local";
        private const string RemoteRoot = "Assets/Content/Remote";

        [MenuItem("Tools/Addressables/Refresh List")]
        public static void SetAddressables()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                EditorUtility.DisplayDialog("Error", "AddressableAssetSettings not found.", "OK");
                return;
            }

            // 1. 모든 경로 수집
            var allPaths = new List<string>();
            if (Directory.Exists(LocalRoot))
                allPaths.AddRange(Directory.GetFiles(LocalRoot, "*.*", SearchOption.AllDirectories));
            if (Directory.Exists(RemoteRoot))
                allPaths.AddRange(Directory.GetFiles(RemoteRoot, "*.*", SearchOption.AllDirectories));

            // 2. 중복 검사용 키 맵
            var keySet = new HashSet<string>();
            var duplicates = new List<string>();

            foreach (var path in allPaths)
            {
                if (path.EndsWith(".meta") || Directory.Exists(path))
                    continue;

                var fileNameKey = Path.GetFileNameWithoutExtension(path);
                if (!keySet.Add(fileNameKey))
                    duplicates.Add(fileNameKey);
            }

            // 3. 중복 키 발생 시 중단
            if (duplicates.Count > 0)
            {
                string msg = "중복된 파일 이름(키)이 발견되어 작업이 중단되었습니다:\n\n" + string.Join("\n", duplicates.Distinct());
                EditorUtility.DisplayDialog("Addressables 설정 실패", msg, "OK");
                return;
            }

            // 4. Addressable 그룹 설정
            ProcessFolder(settings, LocalRoot, isRemote: false);
            ProcessFolder(settings, RemoteRoot, isRemote: true);

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("성공", "Addressables 설정 완료!", "확인");
        }

        private static void ProcessFolder(AddressableAssetSettings settings, string rootPath, bool isRemote)
        {
            if (!Directory.Exists(rootPath))
                return;

            var assetPaths = Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories).Where(path => !path.EndsWith(".meta") && !Directory.Exists(path)).ToArray();

            foreach (var fullPath in assetPaths)
            {
                string assetPath = fullPath.Replace('\\', '/');
                string keyName = Path.GetFileNameWithoutExtension(assetPath); // ✅ 파일명만 키로 사용

                string groupName = isRemote ? $"Remote_{assetPath.Substring(RemoteRoot.Length + 1).Split('/')[0]}" : "Local";

                var group = GetOrCreateGroup(settings, groupName, isRemote);

                var guid = AssetDatabase.AssetPathToGUID(assetPath);
                var entry = settings.CreateOrMoveEntry(guid, group);
                entry.address = keyName;

                string label = null;
                if (isRemote)
                {
                    label = "Remote";
                }
                else
                {
                    label = "Local";
                }

                if (label != null)
                {
                    SetLabel(entry, label);
                }
            }
        }

        private static AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings settings, string groupName, bool isRemote)
        {
            var group = settings.FindGroup(groupName);
            if (group == null)
            {
                group = settings.CreateGroup(groupName, false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            }

            var schema = group.GetSchema<BundledAssetGroupSchema>();
            if (schema == null)
                schema = group.AddSchema<BundledAssetGroupSchema>();

            schema.BuildPath.SetVariableByName(settings, isRemote ? "RemoteBuildPath" : "LocalBuildPath");
            schema.LoadPath.SetVariableByName(settings, isRemote ? "RemoteLoadPath" : "LocalLoadPath");
            schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;

            return group;
        }
        
        private static void SetLabel(AddressableAssetEntry entry, string label)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;

            if (!settings.GetLabels().Contains(label))
            {
                settings.AddLabel(label);
            }

            if (!entry.labels.Contains(label))
            {
                entry.SetLabel(label, true);
                Debug.Log($"라벨 '{label}' → {entry.address}");
            }
        }
    }
}
