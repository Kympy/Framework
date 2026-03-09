using TMPro.EditorUtilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace DragonGate.Editor
{
    [CustomEditor(typeof(LocalizedTextMeshProUGUI), true)]
    public class LocalizedTextMeshProUGUIEditor : TMP_EditorPanelUI
    {
        private SerializedProperty _localizedString;
        private string _previewText;

        protected override void OnEnable()
        {
            base.OnEnable();
            _localizedString = serializedObject.FindProperty("_localizedString");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            EditorGUILayout.PropertyField(_localizedString);
            
            // 로컬라이제이션 시스템 초기화 먼저 완료
            var initOp = LocalizationSettings.InitializationOperation;
            if (!initOp.IsDone)
                initOp.WaitForCompletion();
            // 에디터에서는 SelectedLocale이 null일 수 있으므로 첫 번째 로케일 사용
            var locale = LocalizationSettings.SelectedLocale ?? LocalizationSettings.AvailableLocales?.Locales?[0];
            
            var tmp = (LocalizedTextMeshProUGUI)target;
            if (tmp == null || tmp.LocalizedStringRef.IsEmpty)
            {
                _previewText = null;
            }
            else
            {
                var dataBase = LocalizationSettings.StringDatabase.GetTableEntry(tmp.LocalizedStringRef.TableReference, tmp.LocalizedStringRef.TableEntryReference);
                if (dataBase.Entry.IsSmart)
                {
                    _previewText = dataBase.Entry.Value;
                }
                else
                {
                    _previewText = LocalizationSettings.StringDatabase.GetLocalizedString(
                        tmp.LocalizedStringRef.TableReference,
                        tmp.LocalizedStringRef.TableEntryReference,
                        locale);
                }
            }
            
            EditorGUI.BeginDisabledGroup(true);
            if (string.IsNullOrEmpty(_previewText))
                _previewText = "(not loaded)";
            EditorGUILayout.TextField("Preview", _previewText);
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Apply"))
            {
                if (tmp == null || tmp.LocalizedStringRef.IsEmpty) return;
                
                if (locale == null)
                {
                    Debug.LogWarning("[LocalizedTMP] 사용 가능한 로케일이 없습니다. Localization Settings를 확인하세요.");
                    return;
                }
                tmp.UpdateText(_previewText);
                EditorUtility.SetDirty(tmp);
                Repaint();
            }

            serializedObject.ApplyModifiedProperties();
        }
        
        // ── 씬에 LocalizedTMPUGUI 추가 메뉴 ───────────────────────
        private const string PrefabAssetPath = "Assets/DragonGateCore/Resources/UI/Common/Editor/LocalizedTextMeshProUGUI.prefab";

        [MenuItem("GameObject/UI (Custom)/Localized Text Mesh Pro UGUI", false, 10)]
        private static void CreatePreset(MenuCommand menuCommand)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabAssetPath);
            if (prefab == null)
            {
                Debug.LogError($"Cannot found prefab: {PrefabAssetPath}");
                return;
            }

            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            GameObject  instance;

            if (prefabStage != null)
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, prefabStage.scene);
                instance.transform.SetParent(prefabStage.prefabContentsRoot.transform, false);
            }
            else
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                if (menuCommand.context is GameObject parent)
                    GameObjectUtility.SetParentAndAlign(instance, parent);
            }

            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.UserAction);
            Undo.RegisterCreatedObjectUndo(instance, "CreatePreset_LocalizedTextMeshProUGUI");
            Selection.activeGameObject = instance;
        }
    }
}