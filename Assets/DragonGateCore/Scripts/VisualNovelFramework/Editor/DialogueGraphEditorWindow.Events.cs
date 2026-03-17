using UnityEditor;
using UnityEngine;

namespace DragonGate.Editor
{
    public partial class DialogueGraphEditorWindow
    {
        // 접기 상태
        private bool foldEnterEvents = true;
        private bool foldExitEvents = false;
        
        private static readonly GUIContent s_bgSpriteLabel = new GUIContent("배경 스프라이트");
        private static readonly GUIContent s_positionLabel = new GUIContent("위치");
        private static readonly GUIContent s_easeLabel = new GUIContent("Ease");
        private static readonly GUIContent s_characterLabel = new GUIContent("캐릭터");
        private static readonly GUIContent s_characterScaleLabel = new GUIContent("크기 배율");
        private static readonly GUIContent s_animTriggerLabel = new GUIContent("애니메이션 트리거");
        private static readonly GUIContent s_invertedLabel = new GUIContent("좌우 반전");
        private static readonly GUIContent s_shakeTypeLabel = new GUIContent("Shake 타입");
        private static readonly GUIContent s_shakeStrength = new GUIContent("Shake 강도");
        private static readonly GUIContent s_shakeDuration = new GUIContent("Shake 시간");
        private static readonly GUIContent s_effectPrefabLabel = new GUIContent("이펙트 Prefab");
        private static readonly GUIContent s_effectPositionLabel = new GUIContent("이펙트 Position");
        private static readonly GUIContent s_effectRotationLabel = new GUIContent("이펙트 Rotation");
        private static readonly GUIContent s_uiObjLabel = new GUIContent("UI 오브젝트");
        private static readonly GUIContent s_bgmLabel = new GUIContent("BGM");
        private static readonly GUIContent s_bgmVolumeLabel = new GUIContent("BGM Volume");
        private static readonly GUIContent s_bgmFadeLabel = new GUIContent("BGM Fade Duration");
        private static readonly GUIContent s_sfxLabel = new GUIContent("SFX");
        private static readonly GUIContent s_sfxVolumeLabel = new GUIContent("SFX Volume");
        private static readonly GUIContent s_durationLabel = new GUIContent("시간(초)");
        private static readonly GUIContent s_fadeLabel = new GUIContent("Fade");
        private static readonly GUIContent s_startColorLabel = new GUIContent("시작 색상");
        private static readonly GUIContent s_endColorLabel = new GUIContent("종료 색상");
        private static readonly GUIContent s_waitForCompLabel = new GUIContent("완료 대기");
        
        // ── 이벤트 섹션 ───────────────────────────────────────────────
        //  AssetReference 필드는 SerializedProperty + PropertyField 로,
        //  나머지는 기존 방식 유지.
        // ─────────────────────────────────────────────────────────────

        private void DrawEventsSection(DialogueNode node, string label,
            SerializedObject so,
            string eventsPath,
            ref bool fold)
        {
            fold = EditorGUILayout.BeginFoldoutHeaderGroup(fold, label);
            if (!fold)
            {
                EditorGUILayout.EndFoldoutHeaderGroup();
                return;
            }

            var eventsProp = so.FindProperty(eventsPath);
            if (eventsProp == null)
            {
                EditorGUILayout.HelpBox($"프로퍼티를 찾을 수 없습니다: {eventsPath}", MessageType.Warning);
                EditorGUILayout.EndFoldoutHeaderGroup();
                return;
            }

            for (int i = 0; i < eventsProp.arraySize; i++)
            {
                var evtProp = eventsProp.GetArrayElementAtIndex(i);
                var typeProp = evtProp.FindPropertyRelative("eventType");
                var eventType = (DialogueEventType)typeProp.intValue;

                GUILayout.BeginVertical(EditorStyles.helpBox);

                // ── 헤더: 타입 드롭다운 + 순서 버튼 + 삭제 버튼 ─────────
                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(typeProp, GUIContent.none, GUILayout.Width(160));

                GUI.enabled = i > 0;
                if (GUILayout.Button("▲", GUILayout.Width(22)))
                {
                    eventsProp.MoveArrayElement(i, i - 1);
                    so.ApplyModifiedProperties();
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    break;
                }
                GUI.enabled = i < eventsProp.arraySize - 1;
                if (GUILayout.Button("▼", GUILayout.Width(22)))
                {
                    eventsProp.MoveArrayElement(i, i + 1);
                    so.ApplyModifiedProperties();
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    break;
                }
                GUI.enabled = true;
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("✕", GUILayout.Width(22)))
                {
                    eventsProp.DeleteArrayElementAtIndex(i);
                    so.ApplyModifiedProperties();
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    break;
                }

                GUILayout.EndHorizontal();

                // ── 타입별 필드 ───────────────────────────────────────
                switch (eventType)
                {
                    case DialogueEventType.SetBackground:
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("Background"), s_bgSpriteLabel);
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("Duration"), s_durationLabel);
                        break;

                    case DialogueEventType.ShowCharacter:
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("CharacterAsset"), s_characterLabel);
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("CharacterViewportPosition"), s_positionLabel);
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("CharacterScale"), s_characterScaleLabel);
                        var fadeProp = evtProp.FindPropertyRelative("Fade");
                        EditorGUILayout.PropertyField(fadeProp, s_fadeLabel);
                        if (fadeProp.boolValue)
                        {
                            EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("Duration"), s_durationLabel);
                        }
                        break;
                        
                    case DialogueEventType.MoveCharacter:
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("CharacterAsset"), s_characterLabel);
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("CharacterViewportPosition"), s_positionLabel);
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("CharacterEase"), s_easeLabel);
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("Duration"), s_durationLabel);
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("CharacterScale"), s_characterScaleLabel);
                        break;

                    case DialogueEventType.HideCharacter:
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("CharacterAsset"), s_characterLabel);
                        fadeProp = evtProp.FindPropertyRelative("Fade");
                        EditorGUILayout.PropertyField(fadeProp, s_fadeLabel);
                        if (fadeProp.boolValue)
                        {
                            EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("Duration"), s_durationLabel);
                        }
                        break;
                        
                    case DialogueEventType.InvertCharacter:
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("CharacterAsset"), s_characterLabel);
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("Inverted"), s_invertedLabel);
                        break;
                        
                    case DialogueEventType.ColorCharacter:
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("CharacterAsset"), s_characterLabel);
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("StartColor"), s_startColorLabel);
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("EndColor"), s_endColorLabel); 
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("Duration"), s_durationLabel);
                        break;

                    case DialogueEventType.PlayAnimation:
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("CharacterAsset"), s_characterLabel);
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("AnimationTrigger"), s_animTriggerLabel);
                        break;

                    case DialogueEventType.PlayEffect:
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("FxAsset"), s_effectPrefabLabel);
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("FxViewportPosition"), s_effectPositionLabel);
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("FxRotation"), s_effectRotationLabel);
                        break;
                        
                    case DialogueEventType.Shake:
                        var shakeType = evtProp.FindPropertyRelative("ShakeType");
                        EditorGUILayout.PropertyField(shakeType, s_shakeTypeLabel);
                        if (shakeType.intValue == (int)DialogueShakeType.Character)
                        {
                            EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("CharacterAsset"), s_characterLabel);
                        }
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("ShakeStrength"), s_shakeStrength);
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("Duration"), s_shakeDuration);
                        break;

                    case DialogueEventType.ShowUI:
                    case DialogueEventType.HideUI:
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("UIAsset"), s_uiObjLabel);
                        break;

                    case DialogueEventType.PlayBGM:
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("AudioClip"), s_bgmLabel);
                        var bgmVolumeProp = evtProp.FindPropertyRelative("Volume");
                        EditorGUILayout.Slider(bgmVolumeProp, 0f, 1f, s_bgmVolumeLabel);
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("Duration"), s_bgmFadeLabel);
                        break;
                        
                    case DialogueEventType.BgmVolume:
                        var volumeProp = evtProp.FindPropertyRelative("Volume");
                        EditorGUILayout.Slider(volumeProp, 0f, 1f, s_bgmVolumeLabel);
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("Duration"), s_bgmFadeLabel);
                        break;
                    
                    case DialogueEventType.PlaySFX:
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("AudioClip"), s_sfxLabel);
                        var sfxVolumeProp = evtProp.FindPropertyRelative("Volume");
                        EditorGUILayout.Slider(sfxVolumeProp, 0f, 1f, s_sfxVolumeLabel);
                        break;

                    case DialogueEventType.FadeIn:
                    case DialogueEventType.FadeOut:
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("StartColor"), s_startColorLabel);
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("EndColor"), s_endColorLabel);
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("Duration"), s_durationLabel);
                        break;
                    case DialogueEventType.Wait:
                        EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("Duration"), s_durationLabel);
                        break;
                }


                
                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("WaitForCompletion"), s_waitForCompLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("📋", GUILayout.Width(22)))
                {
                    _copiedEvent = node.EnterEvents[i].Clone();
                    DGDebug.Log($"Copy Event: {_copiedEvent.eventType}", Color.green);
                }
                if (GUILayout.Button("🖋️", GUILayout.Width(22)))
                {
                    if (_copiedEvent == null) return;
                    Undo.RecordObject(_graph, "Paste Event");
                    node.EnterEvents[i] = _copiedEvent.Clone();
                    DGDebug.Log($"Paste Event: {_copiedEvent.eventType}", Color.orange);
                }
                GUILayout.EndHorizontal();
                
                GUILayout.EndVertical();
                GUILayout.Space(3);
            }

            if (GUILayout.Button("+ 이벤트 추가"))
            {
                eventsProp.InsertArrayElementAtIndex(eventsProp.arraySize);
                var insertedNew =  eventsProp.GetArrayElementAtIndex(eventsProp.arraySize - 1);
                var settings = GetOrCreatePreviewSettings();
                insertedNew.FindPropertyRelative("CharacterViewportPosition").vector2Value = settings.DefaultCharacterViewportPosition;
                insertedNew.FindPropertyRelative("CharacterScale").floatValue = settings.DefaultCharacterScale;
                insertedNew.FindPropertyRelative("StartColor").colorValue = settings.DefaultStartColor;
                insertedNew.FindPropertyRelative("EndColor").colorValue = settings.DefaultEndColor;
                so.ApplyModifiedProperties();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}
