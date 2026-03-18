using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using System;

namespace DragonGate
{
    public static partial class AddressableHelper
    {
        private const string LocalRoot = "Assets/Content/Local";
        private const string RemoteRoot = "Assets/Content/Remote";

        // Case-insensitive ignore list
        private static readonly HashSet<string> IgnoreFileList = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".DS_Store",
        };

        private static readonly HashSet<string> IgnoreExtensionList = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".cs",
        };

        // [MenuItem("Tools/Addressables/Refresh List")]
        [MenuItem("Addressables/0. Refresh")]
        public static void SetAddressables()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                EditorUtility.DisplayDialog("Error", "AddressableAssetSettings not found.", "OK");
                return;
            }
            // DGLog.Log(Addressables.BuildPat);

            // EnsureLocalProfile(settings);

            // 1) 수집: Local/Remote 모든 파일 (메타, 디렉토리 제외)
            var allPaths = new List<string>(
                EnumerateAssets(LocalRoot).Concat(EnumerateAssets(RemoteRoot))
            );

            // 2) 중복 키 검사 — 키별로 전체 경로 목록을 수집
            var keyToPathsMap = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (var path in allPaths)
            {
                var key = GetKey(path);
                if (!keyToPathsMap.TryGetValue(key, out var list))
                {
                    list = new List<string>();
                    keyToPathsMap[key] = list;
                }
                list.Add(path);
            }

            var duplicateGroups = keyToPathsMap
                .Where(kv => kv.Value.Count > 1)
                .OrderBy(kv => kv.Key)
                .ToList();
            
            if (duplicateGroups.Count > 0)
            {
                var data = duplicateGroups
                    .Select(kv => (kv.Key, kv.Value))
                    .ToList();

                AddressableDuplicateReportWindow.Show(data);
                return;
            }

            // 3) 그룹/엔트리 반영 + 진행 표시
            var localAssets = EnumerateAssets(LocalRoot).ToList();
            var remoteAssets = EnumerateAssets(RemoteRoot).ToList();
            int total = localAssets.Count + remoteAssets.Count;
            int processed = 0;
            try
            {
                // Local
                foreach (var assetPath in localAssets)
                {
                    float p = total > 0 ? (float)processed / total : 0f;
                    EditorUtility.DisplayProgressBar("Addressables Refresh", $"Local ({processed}/{total}) {Path.GetFileName(assetPath)}", p);
                    ProcessSingleAsset(settings, assetPath, isRemote: false);
                    processed++;
                }

                // Remote
                foreach (var assetPath in remoteAssets)
                {
                    float p = total > 0 ? (float)processed / total : 0f;
                    EditorUtility.DisplayProgressBar("Addressables Refresh", $"Remote ({processed}/{total}) {Path.GetFileName(assetPath)}", p);
                    ProcessSingleAsset(settings, assetPath, isRemote: true);
                    processed++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("성공", "Addressables 설정 완료!", "확인");
        }

        // --- Profile & Play Mode -------------------------------------------------
        // private static void EnsureLocalProfile(AddressableAssetSettings settings)
        // {
        //     var profileId = settings.activeProfileId;
        //     var ps = settings.profileSettings;
        //
        //     void EnsureAndSet(string name, string value)
        //     {
        //         // 변수 존재 보장 (전역 변수)
        //         var getValue = ps.GetValueByName(profileId, name);
        //         if (getValue == null)
        //             ps.CreateValue(name, value);
        //         // 현재 활성 프로필 값 설정
        //         ps.SetValue(profileId, name, value);
        //     }
        //
        //     // StreamingAssets 기반 Local
        //     // EnsureAndSet("Addressables.BuildPath", "Assets/StreamingAssets/aa");
        //     // EnsureAndSet("Addressables.RuntimePath", "{UnityEngine.AddressableAssets.Addressables.RuntimePath}");
        //     // EnsureAndSet("LocalBuildPath", "[UnityEngine.AddressableAssets.Addressables.BuildPath]/[BuildTarget]");
        //     // EnsureAndSet("LocalLoadPath",  "{UnityEngine.AddressableAssets.Addressables.RuntimePath}/[BuildTarget]");
        //     //
        //     // // Remote (테스트 기본값: 로컬 ServerData)
        //     // EnsureAndSet("RemoteBuildPath", "ServerData/[BuildTarget]");
        //     // EnsureAndSet("RemoteLoadPath",  "ServerData/[BuildTarget]");
        // }

        // --- Discovery ----------------------------------------------------------
        private static IEnumerable<string> EnumerateAssets(string root)
        {
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                yield break;

            foreach (var filePath in Directory.GetFiles(root, "*.*", SearchOption.AllDirectories))
            {
                if (filePath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;
                if (Directory.Exists(filePath)) continue; // 방어
                var key = GetKey(filePath);
                // 빈 경로 무시
                if (string.IsNullOrEmpty(key)) continue;
                // 특정 파일 무시
                if (IgnoreFileList.Contains(key)) continue;
                // 확장자 무시
                var extension = Path.GetExtension(filePath);
                if (IgnoreExtensionList.Contains(extension)) continue;
                yield return NormalizePath(filePath);
            }
        }

        private static string NormalizePath(string fullPath) => fullPath.Replace('\\', '/');
        private static string GetKey(string assetPath) => Path.GetFileNameWithoutExtension(assetPath);

        private static void ProcessSingleAsset(AddressableAssetSettings settings, string assetPath, bool isRemote)
        {
            var groupName = isRemote ? GetRemoteGroupName(assetPath) : GetLocalGroupName(assetPath);
            var group = GetOrCreateGroup(settings, groupName, isRemote);

            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            var entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = GetKey(assetPath);

            SetLabel(entry, isRemote ? "Remote" : "Local", false);
        }

        // --- Group / Entry ------------------------------------------------------
        private static void ProcessFolder(AddressableAssetSettings settings, string rootPath, bool isRemote)
        {
            if (!Directory.Exists(rootPath)) return;

            foreach (var assetPath in EnumerateAssets(rootPath))
            {
                var groupName = isRemote ? GetRemoteGroupName(assetPath) : GetLocalGroupName(assetPath);
                var group = GetOrCreateGroup(settings, groupName, isRemote);

                var guid = AssetDatabase.AssetPathToGUID(assetPath);
                var entry = settings.CreateOrMoveEntry(guid, group);
                entry.address = GetKey(assetPath);

                SetLabel(entry, isRemote ? "Remote" : "Local", false);
            }
        }

        private static string GetRemoteGroupName(string assetPath)
        {
            // RemoteRoot/<TopFolder>/... → Remote_<TopFolder>
            var relative = assetPath.Substring(RemoteRoot.Length).TrimStart('/', '\\');
            var top = relative.Split('/')[0];
            return $"Remote_{top}";
        }

        private static string GetLocalGroupName(string assetPath)
        {
            // LocalRoot/<TopFolder>/... → Local_<TopFolder>
            var relative = assetPath.Substring(LocalRoot.Length).TrimStart('/', '\\');
            var top = relative.Split('/')[0];
            return $"Local_{top}";
        }

        private static AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings settings, string groupName, bool isRemote)
        {
            var group = settings.FindGroup(groupName) ??
                        settings.CreateGroup(groupName, false, false, false, null,
                            typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));

            // Ensure schemas exist
            var bundled = group.GetSchema<BundledAssetGroupSchema>() ?? group.AddSchema<BundledAssetGroupSchema>();
            var contentUpdate = group.GetSchema<ContentUpdateGroupSchema>() ?? group.AddSchema<ContentUpdateGroupSchema>();

            // Use the correct profile variable names without dots
            bundled.BuildPath.SetVariableByName(settings, isRemote ? "Remote.BuildPath" : "Local.BuildPath");
            bundled.LoadPath.SetVariableByName(settings, isRemote ? "Remote.LoadPath" : "Local.LoadPath");
            if (isRemote == false)
            {
                bundled.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            }

            // Optional: ensure content update schema default
            contentUpdate.StaticContent = false; // allow updates by default
            return group;
        }

        private static void SetLabel(AddressableAssetEntry entry, string label, bool clear)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings.GetLabels().Contains(label))
                settings.AddLabel(label);

            if (clear == false)
            {
                if (!entry.labels.Contains(label))
                    entry.SetLabel(label, true);
            }
            else
            {
                entry.labels.Clear();
                entry.SetLabel(label, true);
            }
        }
    }
}
