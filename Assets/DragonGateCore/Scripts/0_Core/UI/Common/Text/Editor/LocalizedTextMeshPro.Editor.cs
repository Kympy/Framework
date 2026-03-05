using TMPro.EditorUtilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace DragonGate.Editor
{
    [CustomEditor(typeof(LocalizedTextMeshPro), true)]
    public class LocalizedTextMeshProEditor : TMP_EditorPanel
    {
        private SerializedProperty _localizedString;

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

            var locale = LocalizationSettings.SelectedLocale
                         ?? LocalizationSettings.AvailableLocales?.Locales?[0];
            EditorGUI.BeginDisabledGroup(true);
            var preview = ((LocalizedTextMeshPro)target).GetLocalizedString() ?? "(not loaded)";
            EditorGUILayout.TextField("Preview", preview);
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Apply"))
            {
                var tmp = (LocalizedTextMeshPro)target;
                if (tmp == null || tmp.LocalizedStringRef.IsEmpty) return;

                var initOp = LocalizationSettings.InitializationOperation;
                if (!initOp.IsDone)
                    initOp.WaitForCompletion();

                if (locale == null)
                {
                    Debug.LogWarning("[LocalizedTMP] 사용 가능한 로케일이 없습니다. Localization Settings를 확인하세요.");
                    return;
                }

                var handle = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(
                    tmp.LocalizedStringRef.TableReference,
                    tmp.LocalizedStringRef.TableEntryReference,
                    locale);
                handle.WaitForCompletion();

                if (handle.Result == null)
                {
                    Debug.LogWarning($"[LocalizedTMP] 문자열 로드 실패. Table: {tmp.LocalizedStringRef.TableReference}, Entry: {tmp.LocalizedStringRef.TableEntryReference}, Locale: {locale.Identifier}");
                    return;
                }

                tmp.UpdateText(handle.Result);
                EditorUtility.SetDirty(tmp);
                Repaint();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}