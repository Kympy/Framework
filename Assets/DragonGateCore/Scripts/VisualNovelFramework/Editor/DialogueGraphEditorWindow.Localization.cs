using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace DragonGate.Editor
{
    public partial class DialogueGraphEditorWindow
    {
        // 대사 로컬 테이블
        private const string LOCALIZATION_DIALOGUE_TABLE = "Dialogue";
        private const string LOCALIZATION_CHOICE_TABLE = "Choice";
        private const string LOCALIZATION_DIALOGUE_KEY_FORMAT = "Node_Dialogue_{0}";
        private const string LOCALIZATION_CHOICE_KEY_FORMAT = "Choice_{0}";
        
        // 언어
        private void RefreshLocales()
        {
            if (LocalizationSettings.AvailableLocales == null)
            {
                var initOperation = LocalizationSettings.InitializationOperation;
                if (initOperation.IsDone == false) initOperation.WaitForCompletion();
            }
            var locales = LocalizationSettings.AvailableLocales?.Locales;
            if (locales == null || locales.Count == 0)
            {
                _localeNames = new[] { "No Locales" };
                _selectedLocaleIdx = -1;
                return;
            }

            _localeNames = locales.Select(l => l.LocaleName).ToArray();

            // 현재 선택된 Locale에 맞게 인덱스 동기화
            var current = LocalizationSettings.SelectedLocale;
            if (current != null)
            {
                _selectedLocaleIdx = locales.IndexOf(current);
            }
            else
            {
                _selectedLocaleIdx = -1;
            }
        }
        
        // ── Localization 헬퍼 ─────────────────────────────────────────

        private LocalizedString AddLocalizationKey(string tableName, string key)
        {
            var col = LocalizationEditorSettings.GetStringTableCollection(tableName);
            if (col == null)
            {
                col = LocalizationEditorSettings.CreateStringTableCollection(tableName, $"Assets/Content/Local/Localization/Table/{tableName}");
                DGDebug.Log($"StringTableCollection 생성: {tableName}", Color.antiqueWhite);
            }
            if (col.SharedData.GetEntry(key) == null)
            {
                col.SharedData.AddKey(key);
                EditorUtility.SetDirty(col.SharedData);
                
                foreach (var table in col.StringTables)
                {
                    table.AddEntry(key, "");
                    EditorUtility.SetDirty(table);
                }
            }
            EditorUtility.SetDirty(col);
            AssetDatabase.SaveAssets();
            DGDebug.Log($"Localization Key 추가. {tableName} / {key}", Color.azure);
            return new LocalizedString(tableName, key);
        }

        private void RemoveLocalizationKey(string tableName, string key)
        {
            var col = LocalizationEditorSettings.GetStringTableCollection(tableName);
            if (col == null) return;

            // ✅ SharedData에서 키 제거 (키 정의 삭제)
            col.SharedData.RemoveKey(key);
            EditorUtility.SetDirty(col.SharedData); // 더티 처리 무조건 해야함. 그래야 저장됨. -> 로케일이랑 SharedData랑 에셋이 따로임.
            
            // ✅ 각 언어 테이블도 개별 dirty 처리
            foreach (var table in col.StringTables)
            {
                table.RemoveEntry(key); // 혹시 값이 남아있을 경우 대비
                EditorUtility.SetDirty(table);
            }

            EditorUtility.SetDirty(col);
            AssetDatabase.SaveAssets();

            DGDebug.Log($"Localization Key 제거. {tableName} / {key}", Color.orange);
        }

        // 노드 생성 시 자동으로 테이블 엔트리 생성
        private void CreateLocalizationEntries(DialogueNode node)
        {
            if (node == null) { DGDebug.LogError("Node is null"); return; }
            if (node.NodeType == DialogueNodeType.Start || node.NodeType == DialogueNodeType.ChapterEnd || node.NodeType == DialogueNodeType.Condition) return;
            if (string.IsNullOrEmpty(_graph.GraphTitle)) { DGDebug.LogError("Graph Title is null or empty!!"); return; }

            string key = string.Format(LOCALIZATION_DIALOGUE_KEY_FORMAT, node.nodeId);
            var localized = AddLocalizationKey(LOCALIZATION_DIALOGUE_TABLE, key);
            if (node.DialogueText == null || node.DialogueText.IsEmpty)
                node.DialogueText = localized;
        }

        // 노드 삭제 시 테이블 엔트리도 정리 (Choice 키 포함)
        private void RemoveLocalizationEntries(DialogueNode node)
        {
            if (node == null) { DGDebug.LogError("Node is null!"); return; }
            RemoveLocalizationKey(LOCALIZATION_DIALOGUE_TABLE, string.Format(LOCALIZATION_DIALOGUE_KEY_FORMAT, node.nodeId));
            if (node.Choices == null) return;
            foreach (var choice in node.Choices)
                RemoveLocalizationKey(LOCALIZATION_CHOICE_TABLE, string.Format(LOCALIZATION_CHOICE_KEY_FORMAT, choice.Id));
        }
    }
}
