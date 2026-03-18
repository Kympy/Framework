using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DragonGate
{
    public class AddressableDuplicateReportWindow : EditorWindow
    {
        // (키, 경로 목록) 쌍
        private List<(string key, List<string> paths)> _duplicates;
        private Vector2 _scroll;

        private static readonly Color HeaderBg    = new Color(0.85f, 0.25f, 0.20f, 1f); // 붉은 헤더
        private static readonly Color RowEven     = new Color(0.18f, 0.18f, 0.18f, 1f);
        private static readonly Color RowOdd      = new Color(0.22f, 0.22f, 0.22f, 1f);
        private static readonly Color KeyColor    = new Color(1.00f, 0.85f, 0.30f, 1f); // 노란 키
        private static readonly Color PathColor   = new Color(0.75f, 0.90f, 1.00f, 1f); // 하늘색 경로

        public static void Show(List<(string key, List<string> paths)> duplicates)
        {
            var win = GetWindow<AddressableDuplicateReportWindow>(true, "Addressables — 중복 키 발견", true);
            win.minSize = new Vector2(640, 420);
            win._duplicates = duplicates;
            win._scroll = Vector2.zero;
            win.Show();
        }

        private void OnGUI()
        {
            if (_duplicates == null) return;

            // ── 상단 요약 헤더 ──────────────────────────────────
            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = HeaderBg;
            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUI.backgroundColor = prevBg;
                EditorGUILayout.LabelField(
                    $"⚠  중복된 키 {_duplicates.Count}건이 발견되었습니다 — 수동으로 파일 이름을 수정 후 다시 실행하세요.",
                    new GUIStyle(EditorStyles.boldLabel) { wordWrap = true, fontSize = 12 }
                );
            }
            GUI.backgroundColor = prevBg;

            GUILayout.Space(4);

            // ── 스크롤 본문 ────────────────────────────────────
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            for (int i = 0; i < _duplicates.Count; i++)
            {
                var (key, paths) = _duplicates[i];

                // 그룹 배경
                var rowBg = i % 2 == 0 ? RowEven : RowOdd;
                var rect = EditorGUILayout.BeginVertical();
                EditorGUI.DrawRect(rect, rowBg);

                // 키 라벨
                GUILayout.Space(4);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(8);
                    EditorGUILayout.LabelField($"[{i + 1}]", GUILayout.Width(30));
                    EditorGUILayout.LabelField("키 :", GUILayout.Width(28));
                    EditorGUILayout.LabelField(key,
                        new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = KeyColor } });
                }

                // 충돌 경로 목록
                foreach (var path in paths)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(48);

                        // 경로 텍스트
                        EditorGUILayout.LabelField("•  " + path,
                            new GUIStyle(EditorStyles.label)
                            {
                                normal   = { textColor = PathColor },
                                wordWrap = true
                            });

                        // 해당 파일 핑(Ping) 버튼
                        if (GUILayout.Button("Ping", GUILayout.Width(44)))
                            EditorGUIUtility.PingObject(
                                AssetDatabase.LoadMainAssetAtPath(path));
                    }
                }

                GUILayout.Space(4);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();

            // ── 하단 버튼 ──────────────────────────────────────
            GUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                // GUILayout.FlexibleSpace();
                if (GUILayout.Button("Console에 전체 출력", GUILayout.Height(28)))
                    LogToConsole();
                if (GUILayout.Button("Refresh", GUILayout.Height(28)))
                    AddressableHelper.SetAddressables();

                if (GUILayout.Button("닫기", GUILayout.Height(28), GUILayout.Width(80)))
                    Close();
            }
            GUILayout.Space(4);
        }

        private void LogToConsole()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[Addressables] 중복 키 {_duplicates.Count}건:");
            foreach (var (key, paths) in _duplicates)
            {
                sb.AppendLine($"  키: \"{key}\"");
                foreach (var p in paths)
                    sb.AppendLine($"    • {p}");
            }
            Debug.LogError(sb.ToString());
        }
    }
}