#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace DragonGate.Editor
{
    public class LocalizationCSVWindow : EditorWindow
    {
        // ── 상태 ─────────────────────────────────────────────────────
        private List<StringTableCollection> _allCollections = new();
        private string[]                    _collectionNames;
        private int                         _selectedIdx;
        private StringTableCollection       _selectedCollection;

        private Vector2 _scrollPos;
        private string  _lastResult;
        private bool    _lastSuccess;

        // ── 탭 ───────────────────────────────────────────────────────
        private int _tab; // 0 = Export, 1 = Import
        private static readonly string[] TAB_LABELS = { "📤 Export", "📥 Import" };

        // ── Import 미리보기 ──────────────────────────────────────────
        private string   _importPath;
        private string[] _importPreviewLines;
        private bool     _importOverwrite;

        [MenuItem("DragonGate/Localization/CSV Export·Import")]
        public static void Open()
        {
            var w = GetWindow<LocalizationCSVWindow>("Localization CSV");
            w.minSize = new Vector2(480, 400);
            w.RefreshCollections();
        }

        // ── 컬렉션 목록 갱신 ─────────────────────────────────────────
        private void RefreshCollections()
        {
            _allCollections.Clear();
            var guids = AssetDatabase.FindAssets("t:StringTableCollection");
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var col  = AssetDatabase.LoadAssetAtPath<StringTableCollection>(path);
                if (col != null) _allCollections.Add(col);
            }

            _collectionNames = new string[_allCollections.Count];
            for (int i = 0; i < _allCollections.Count; i++)
                _collectionNames[i] = _allCollections[i].TableCollectionName;

            _selectedIdx        = 0;
            _selectedCollection = _allCollections.Count > 0 ? _allCollections[0] : null;
            _lastResult         = null;
        }

        // ══════════════════════════════════════════════════════════════
        //  OnGUI
        // ══════════════════════════════════════════════════════════════
        private void OnGUI()
        {
            DrawHeader();

            if (_allCollections.Count == 0)
            {
                EditorGUILayout.HelpBox("프로젝트에 StringTableCollection이 없습니다.", MessageType.Warning);
                if (GUILayout.Button("새로고침")) RefreshCollections();
                return;
            }

            DrawCollectionSelector();
            GUILayout.Space(6);

            _tab = GUILayout.Toolbar(_tab, TAB_LABELS);
            GUILayout.Space(6);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            if (_tab == 0) DrawExportTab();
            else           DrawImportTab();
            EditorGUILayout.EndScrollView();

            DrawResultBox();
        }

        // ── 헤더 ─────────────────────────────────────────────────────
        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("🌐  Localization CSV Tool", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("↺ 새로고침", EditorStyles.toolbarButton, GUILayout.Width(80)))
                RefreshCollections();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(4);
        }

        // ── 컬렉션 선택 ──────────────────────────────────────────────
        private void DrawCollectionSelector()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("테이블 선택", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _selectedIdx = EditorGUILayout.Popup("StringTableCollection", _selectedIdx, _collectionNames);
            if (EditorGUI.EndChangeCheck())
            {
                _selectedCollection = _allCollections[_selectedIdx];
                _importPreviewLines = null;
                _lastResult         = null;
            }

            if (_selectedCollection != null)
            {
                var tables = _selectedCollection.StringTables;
                var localeStr = new StringBuilder();
                foreach (var t in tables)
                    localeStr.Append($"  [{t.LocaleIdentifier.Code}]");
                EditorGUILayout.LabelField("포함 Locale", localeStr.ToString(), EditorStyles.miniLabel);
                EditorGUILayout.LabelField("키 수",       _selectedCollection.SharedData.Entries.Count.ToString(), EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        // ══════════════════════════════════════════════════════════════
        //  Export 탭
        // ══════════════════════════════════════════════════════════════
        private void DrawExportTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("CSV 내보내기", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "UTF-8 BOM 형식으로 저장합니다.\nExcel·Google Sheets에서 한글이 정상 표시됩니다.",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("📤  내보내기...", GUILayout.Height(30)))
                DoExport();

            EditorGUILayout.EndVertical();
        }

        private void DoExport()
        {
            if (_selectedCollection == null) return;

            string defaultName = _selectedCollection.TableCollectionName;
            string path = EditorUtility.SaveFilePanel("CSV 내보내기", "", defaultName, "csv");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                var sb     = new StringBuilder();
                var tables = _selectedCollection.StringTables;

                // 헤더
                sb.Append("Key");
                foreach (var t in tables)
                    sb.Append($",{t.LocaleIdentifier.Code}");
                sb.AppendLine();

                // 엔트리
                foreach (var entry in _selectedCollection.SharedData.Entries)
                {
                    sb.Append(EscapeCSV(entry.Key));
                    foreach (var t in tables)
                    {
                        var le = t.GetEntry(entry.Id);
                        sb.Append($",{EscapeCSV(le?.LocalizedValue ?? "")}");
                    }
                    sb.AppendLine();
                }

                // ✅ UTF-8 BOM
                File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));

                _lastSuccess = true;
                _lastResult  = $"✅ 내보내기 완료\n{path}\n{_selectedCollection.SharedData.Entries.Count}개 키";
                
                EditorUtility.RevealInFinder(path);
            }
            catch (System.Exception e)
            {
                _lastSuccess = false;
                _lastResult  = $"❌ 오류: {e.Message}";
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  Import 탭
        // ══════════════════════════════════════════════════════════════
        private void DrawImportTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("CSV 가져오기", EditorStyles.boldLabel);

            // 파일 선택
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField("파일 경로", string.IsNullOrEmpty(_importPath) ? "(선택 안됨)" : _importPath);
            if (GUILayout.Button("찾기", GUILayout.Width(44)))
            {
                string p = EditorUtility.OpenFilePanel("CSV 가져오기", "", "csv");
                if (!string.IsNullOrEmpty(p))
                {
                    _importPath         = p;
                    _importPreviewLines = null;
                    LoadImportPreview();
                }
            }
            EditorGUILayout.EndHorizontal();

            // 옵션
            _importOverwrite = EditorGUILayout.Toggle(
                new GUIContent("기존 값 덮어쓰기", "체크 해제 시 비어있는 값만 채웁니다"),
                _importOverwrite);

            // 미리보기
            if (_importPreviewLines != null && _importPreviewLines.Length > 0)
            {
                GUILayout.Space(4);
                GUILayout.Label("미리보기 (상위 5행)", EditorStyles.miniLabel);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                int count = Mathf.Min(5, _importPreviewLines.Length);
                for (int i = 0; i < count; i++)
                    GUILayout.Label(_importPreviewLines[i], EditorStyles.miniLabel);
                if (_importPreviewLines.Length > 5)
                    GUILayout.Label($"... 외 {_importPreviewLines.Length - 5}행", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
            }

            GUILayout.Space(4);

            GUI.enabled = !string.IsNullOrEmpty(_importPath) && _selectedCollection != null;
            if (GUILayout.Button("📥  가져오기", GUILayout.Height(30)))
                DoImport();
            GUI.enabled = true;

            EditorGUILayout.EndVertical();
        }

        private void LoadImportPreview()
        {
            try
            {
                var lines = File.ReadAllLines(_importPath, new UTF8Encoding(true));
                _importPreviewLines = lines;
            }
            catch { _importPreviewLines = null; }
        }

        private void DoImport()
        {
            if (string.IsNullOrEmpty(_importPath) || _selectedCollection == null) return;

            try
            {
                var lines = File.ReadAllLines(_importPath, new UTF8Encoding(true));
                if (lines.Length < 2)
                {
                    _lastSuccess = false;
                    _lastResult  = "❌ CSV가 비어있습니다.";
                    return;
                }

                // 헤더에서 locale 순서 파악
                string[] headers = lines[0].Split(',');

                // locale code → StringTable 매핑
                var tableMap = new Dictionary<string, StringTable>();
                foreach (var t in _selectedCollection.StringTables)
                    tableMap[t.LocaleIdentifier.Code] = t as StringTable;

                int updated = 0, skipped = 0, added = 0;

                for (int i = 1; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i])) continue;

                    string[] cols = SplitCSVLine(lines[i]);
                    if (cols.Length < 1) continue;

                    string key = cols[0];
                    if (string.IsNullOrEmpty(key)) continue;

                    // 키 없으면 SharedData에 추가
                    if (_selectedCollection.SharedData.GetEntry(key) == null)
                    {
                        _selectedCollection.SharedData.AddKey(key);
                        added++;
                    }

                    for (int j = 1; j < headers.Length; j++)
                    {
                        if (j >= cols.Length) continue;

                        string code  = headers[j].Trim();
                        string value = cols[j];

                        if (!tableMap.TryGetValue(code, out var table)) continue;

                        var existing = table.GetEntry(key);

                        if (existing != null && !string.IsNullOrEmpty(existing.LocalizedValue) && !_importOverwrite)
                        {
                            skipped++;
                            continue;
                        }

                        table.AddEntry(key, value);
                        updated++;
                    }
                }

                EditorUtility.SetDirty(_selectedCollection);
                AssetDatabase.SaveAssets();

                _lastSuccess = true;
                _lastResult  = $"✅ 가져오기 완료\n신규 키: {added}개 / 업데이트: {updated}개 / 스킵: {skipped}개";
            }
            catch (System.Exception e)
            {
                _lastSuccess = false;
                _lastResult  = $"❌ 오류: {e.Message}";
            }
        }

        // ── 결과 박스 ────────────────────────────────────────────────
        private void DrawResultBox()
        {
            if (string.IsNullOrEmpty(_lastResult)) return;

            GUILayout.Space(4);
            EditorGUILayout.HelpBox(_lastResult, _lastSuccess ? MessageType.Info : MessageType.Error);
        }

        // ── 유틸 ─────────────────────────────────────────────────────
        private static string EscapeCSV(string v)
        {
            if (string.IsNullOrEmpty(v)) return "";
            if (v.Contains(",") || v.Contains("\"") || v.Contains("\n"))
                return $"\"{v.Replace("\"", "\"\"")}\"";
            return v;
        }

        // 따옴표로 감싼 필드 처리하는 CSV 파서
        private static string[] SplitCSVLine(string line)
        {
            var result  = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                    { current.Append('"'); i++; }
                    else if (c == '"')
                        inQuotes = false;
                    else
                        current.Append(c);
                }
                else
                {
                    if (c == '"')      inQuotes = true;
                    else if (c == ',') { result.Add(current.ToString()); current.Clear(); }
                    else               current.Append(c);
                }
            }
            result.Add(current.ToString());
            return result.ToArray();
        }
    }
}
#endif