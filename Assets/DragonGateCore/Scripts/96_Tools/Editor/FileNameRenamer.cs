using System.IO;
using UnityEditor;
using UnityEngine;

namespace DragonGate
{
    public class FileNameRenamer : EditorWindow
    {
        private enum RenameMode
        {
            SelectedFiles,
            FolderFiles
        }

        private RenameMode _mode = RenameMode.SelectedFiles;
        private Object _targetFolder;

        private string _prefix = string.Empty;
        private string _suffix = string.Empty;
        private string _searchString = string.Empty;
        private string _replaceString = string.Empty;

        private const int MaxPreviewCount = 10;
        private Vector2 _scrollPosition;

        [MenuItem("Assets/Rename/File Name Renamer")]
        private static void OpenWindow()
        {
            var window = GetWindow<FileNameRenamer>("File Name Renamer");
            window.minSize = new Vector2(400, 250);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("파일 이름 일괄 변경 도구", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _mode = (RenameMode)EditorGUILayout.EnumPopup("작업 대상", _mode);
            EditorGUILayout.Space();

            if (_mode == RenameMode.FolderFiles)
            {
                _targetFolder = EditorGUILayout.ObjectField("대상 폴더", _targetFolder, typeof(DefaultAsset), false);
                EditorGUILayout.HelpBox("폴더 내 모든 파일에 적용됩니다 (하위 폴더 제외)", MessageType.Info);
            }
            else
            {
                DrawSelectedFilesPreview();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("변경 옵션", EditorStyles.boldLabel);

            _prefix = EditorGUILayout.TextField("접두사 추가", _prefix);
            _suffix = EditorGUILayout.TextField("접미사 추가", _suffix);

            EditorGUILayout.Space();

            _searchString = EditorGUILayout.TextField("찾을 문자열", _searchString);
            _replaceString = EditorGUILayout.TextField("대치 문자열", _replaceString);

            EditorGUILayout.Space();

            GUI.enabled = IsReadyToExecute();
            if (GUILayout.Button("실행", GUILayout.Height(30)))
            {
                Execute();
            }
            GUI.enabled = true;
        }

        private void DrawSelectedFilesPreview()
        {
            var selectedGuids = Selection.assetGUIDs;
            int fileCount = 0;

            if (selectedGuids != null && selectedGuids.Length > 0)
            {
                foreach (var guid in selectedGuids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (!AssetDatabase.IsValidFolder(assetPath))
                        fileCount++;
                }
            }

            if (fileCount == 0)
            {
                EditorGUILayout.HelpBox("선택된 파일이 없습니다", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField($"선택된 파일: {fileCount}개", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(100));

            int displayCount = 0;
            foreach (var guid in selectedGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                if (AssetDatabase.IsValidFolder(assetPath))
                    continue;

                if (displayCount >= MaxPreviewCount)
                    break;

                string fileName = Path.GetFileName(assetPath);
                EditorGUILayout.LabelField($"• {fileName}", EditorStyles.miniLabel);
                displayCount++;
            }

            if (fileCount > MaxPreviewCount)
            {
                EditorGUILayout.LabelField($"... 외 {fileCount - MaxPreviewCount}개 더", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private bool IsReadyToExecute()
        {
            if (_mode == RenameMode.FolderFiles)
            {
                return _targetFolder != null && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(_targetFolder));
            }

            return Selection.assetGUIDs != null && Selection.assetGUIDs.Length > 0;
        }

        private void Execute()
        {
            bool hasPrefix = !string.IsNullOrEmpty(_prefix);
            bool hasSuffix = !string.IsNullOrEmpty(_suffix);
            bool hasSearch = !string.IsNullOrEmpty(_searchString);

            if (!hasPrefix && !hasSuffix && !hasSearch)
            {
                UnityEngine.Debug.LogError("적어도 하나의 변경 옵션을 입력해야 합니다.");
                return;
            }

            if (_mode == RenameMode.FolderFiles)
            {
                ExecuteForFolder();
            }
            else
            {
                ExecuteForSelectedFiles();
            }
        }

        private void ExecuteForSelectedFiles()
        {
            var selectedGuids = Selection.assetGUIDs;

            if (selectedGuids == null || selectedGuids.Length == 0)
            {
                UnityEngine.Debug.LogError("선택된 파일이 없습니다.");
                return;
            }

            int renameCount = 0;

            foreach (var guid in selectedGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                if (AssetDatabase.IsValidFolder(assetPath))
                    continue;

                string oldFileName = Path.GetFileNameWithoutExtension(assetPath);
                string newFileName = ProcessFileName(oldFileName);

                if (oldFileName == newFileName)
                    continue;

                string result = AssetDatabase.RenameAsset(assetPath, newFileName);

                if (string.IsNullOrEmpty(result))
                    renameCount++;
                else
                    UnityEngine.Debug.LogError($"Rename 실패: {assetPath} / {result}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            UnityEngine.Debug.Log($"✅ 총 {renameCount}개 파일 이름 변경 완료.");
        }

        private void ExecuteForFolder()
        {
            string folderPath = AssetDatabase.GetAssetPath(_targetFolder);

            if (!Directory.Exists(folderPath))
            {
                UnityEngine.Debug.LogError("선택한 항목이 폴더가 아닙니다.");
                return;
            }

            string[] files = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly);
            int renameCount = 0;

            foreach (var file in files)
            {
                if (file.EndsWith(".meta"))
                    continue;

                string assetPath = file.Replace(Application.dataPath, "Assets").Replace("\\", "/");
                string oldFileName = Path.GetFileNameWithoutExtension(assetPath);
                string extension = Path.GetExtension(assetPath);
                string newFileName = ProcessFileName(oldFileName);

                if (oldFileName == newFileName)
                    continue;

                string directory = Path.GetDirectoryName(assetPath);
                string newPath = Path.Combine(directory, newFileName + extension).Replace("\\", "/");

                if (AssetDatabase.MoveAsset(assetPath, newPath) == string.Empty)
                {
                    renameCount++;
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"⚠️ 파일 이동 실패: {oldFileName}");
                }
            }

            AssetDatabase.Refresh();
            UnityEngine.Debug.Log($"✅ {renameCount}개의 파일 이름 변경 완료.");
        }

        private string ProcessFileName(string fileName)
        {
            string result = fileName;

            if (!string.IsNullOrEmpty(_searchString))
            {
                result = result.Replace(_searchString, _replaceString);
            }

            if (!string.IsNullOrEmpty(_prefix))
            {
                result = _prefix + result;
            }

            if (!string.IsNullOrEmpty(_suffix))
            {
                result = result + _suffix;
            }

            return result;
        }
    }
}
