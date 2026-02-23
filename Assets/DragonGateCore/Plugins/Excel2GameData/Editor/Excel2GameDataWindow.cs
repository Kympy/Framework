using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using Excel2GameData; // Generator 클래스 네임스페이스

public class Excel2GameDataWindow : EditorWindow
{
    // 다중 선택된 Excel 파일 경로를 저장할 리스트
    private List<string> _excelFilePaths = new List<string>();
    private static string _excelFolderPath = "";
    private static string _dataResourceOutputFolder;
    private static string _classOutputFolder;

    // EditorPrefs 키
    private const string PREFS_EXCEL_FOLDER = "Excel2Json_ExcelFolder";
    private const string PREFS_DATA_RESOURCE_FOLDER  = "Excel2Json_JsonOutputFolder";
    private const string PREFS_CLASS_FOLDER = "Excel2Json_ClassOutputFolder";

    // 드래그&드롭 영역 높이
    private const float DragAreaHeight = 60f;

    [MenuItem("Tools/Data/Excel→GameData")]
    public static void ShowWindow()
    {
        GetWindow<Excel2GameDataWindow>("Excel→GameData");
    }

    [MenuItem("Tools/Data/엑셀 폴더 열기")]
    public static void OpenExcelFolder()
    {
        if (string.IsNullOrEmpty(_excelFolderPath))
        {
            var window = GetWindow<Excel2GameDataWindow>("Excel→GameData");
            window.LoadSettings();
            window.Close();
        }
        EditorUtility.RevealInFinder(_excelFolderPath);
    }

    public static void OnOneClick()
    {
        var window = GetWindow<Excel2GameDataWindow>("Excel→GameData");
        window.Show();
        
        if (!string.IsNullOrEmpty(_excelFolderPath))
        {
            // 폴더에서 .xlsx 파일 취합
            string[] filesInFolder = Directory.GetFiles(_excelFolderPath, "*.xlsx", SearchOption.TopDirectoryOnly);
            if (filesInFolder.Length > 0)
            {
                ExecuteGenerate(filesInFolder);
            }
            else
            {
                UnityEngine.Debug.LogWarning($"폴더 '{_excelFolderPath}'에 .xlsx 파일이 없습니다.");
            }
        }
        window.Close();
    }

    private void OnEnable()
    {
        LoadSettings();
    }

    private void LoadSettings()
    {
        // 이전에 저장된 값이 있으면 불러오고, 없으면 기본값 설정
        _excelFolderPath = EditorPrefs.GetString(PREFS_EXCEL_FOLDER, "");
        _dataResourceOutputFolder  = EditorPrefs.GetString(PREFS_DATA_RESOURCE_FOLDER,  "Assets/Content/Local/GameData");
        _classOutputFolder = EditorPrefs.GetString(PREFS_CLASS_FOLDER, "Assets/Scripts/GameData/Generated");
    }

    private void OnGUI()
    {
        GUILayout.Label("Excel → GameData + 클래스 자동 생성", EditorStyles.boldLabel);
        GUILayout.Space(8);

        // ── 1) 다중 파일 선택 영역 ──────────────────────────────────────────
        GUILayout.Label("① 엑셀 파일 선택 (다중 선택 가능)", EditorStyles.boldLabel);

        // 파일 경로 리스트 출력
        if (_excelFilePaths.Count > 0)
        {
            for (int i = 0; i < _excelFilePaths.Count; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(Path.GetFileName(_excelFilePaths[i]), GUILayout.Width(200));
                if (GUILayout.Button("제거", GUILayout.Width(50)))
                {
                    _excelFilePaths.RemoveAt(i);
                    i--;
                }
                GUILayout.EndHorizontal();
            }
        }
        else
        {
            GUILayout.Label("선택된 파일이 없습니다.");
        }

        // “파일 찾아보기” 버튼: 한 번에 하나씩만 선택 가능
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("파일 찾아보기", GUILayout.Height(30), GUILayout.Width(100)))
        {
            string path = EditorUtility.OpenFilePanelWithFilters(
                "Excel(.xlsx) 파일 선택", 
                Application.dataPath, 
                new string[] { "Excel 파일", "xlsx", "모든 파일", "*" }
            );

            if (!string.IsNullOrEmpty(path) 
                && Path.GetExtension(path).Equals(".xlsx", System.StringComparison.OrdinalIgnoreCase))
            {
                if (!_excelFilePaths.Contains(path))
                    _excelFilePaths.Add(path);
            }
        }

        if (GUILayout.Button("모두 삭제", GUILayout.Height(30), GUILayout.Width(80)))
        {
            _excelFilePaths.Clear();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(6);

        // ── 2) 드래그 앤 드롭 영역 ─────────────────────────────────────────
        var dragAreaRect = GUILayoutUtility.GetRect(
            0, DragAreaHeight, 
            GUILayout.ExpandWidth(true)
        );
        GUI.Box(dragAreaRect, "여기에 .xlsx 파일을 드래그하여 추가할 수 있습니다.", EditorStyles.helpBox);
        HandleDragAndDrop(dragAreaRect);

        GUILayout.Space(12);

        // ── 3) 엑셀 폴더 전체 선택할 때 ────────────────────────────────────────
        GUILayout.Label("② 엑셀 폴더 선택 (폴더 내 모든 .xlsx 처리)", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Excel 폴더:", GUILayout.Width(100));
        string newExcelFolderPath = GUILayout.TextField(_excelFolderPath);
        if (newExcelFolderPath != _excelFolderPath)
        {
            _excelFolderPath = newExcelFolderPath;
            EditorPrefs.SetString(PREFS_EXCEL_FOLDER, _excelFolderPath);
        }
        if (GUILayout.Button("폴더 찾아보기", GUILayout.Width(80)))
        {
            string folder = EditorUtility.OpenFolderPanel("Excel 파일이 들어있는 폴더 선택", Application.dataPath, "");
            if (!string.IsNullOrEmpty(folder))
            {
                _excelFolderPath = folder;
                EditorPrefs.SetString(PREFS_EXCEL_FOLDER, _excelFolderPath);
                _excelFilePaths.Clear(); // 파일 리스트 초기화
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(12);
        EditorGUILayout.HelpBox(
            "파일 목록이 있을 경우 파일 우선 처리됩니다.\n" +
            "폴더가 지정되면 해당 폴더 내 모든 .xlsx 파일을 처리합니다.", 
            MessageType.Info
        );

        GUILayout.Space(12);

        // ── 4) JSON/클래스 저장 폴더 지정 ──────────────────────────────────
        GUILayout.Label("③ 출력 폴더 설정", EditorStyles.boldLabel);

        // Data Resource 출력 폴더
        GUILayout.BeginHorizontal();
        GUILayout.Label("Data Resource 저장 폴더:", GUILayout.Width(100));
        string newJsonFolder = GUILayout.TextField(_dataResourceOutputFolder);
        if (newJsonFolder != _dataResourceOutputFolder)
        {
            _dataResourceOutputFolder = newJsonFolder;
            EditorPrefs.SetString(PREFS_DATA_RESOURCE_FOLDER, _dataResourceOutputFolder);
        }
        if (GUILayout.Button("폴더 선택", GUILayout.Width(80)))
        {
            string folder = EditorUtility.OpenFolderPanel("Data Resource 저장 폴더 선택", Application.dataPath, "");
            if (!string.IsNullOrEmpty(folder))
            {
                if (folder.StartsWith(Application.dataPath))
                {
                    _dataResourceOutputFolder = "Assets" + folder.Substring(Application.dataPath.Length);
                    EditorPrefs.SetString(PREFS_DATA_RESOURCE_FOLDER, _dataResourceOutputFolder);
                }
                else
                {
                    UnityEngine.Debug.LogError("Data Resource 저장 폴더는 반드시 Assets 폴더 내부여야 합니다.");
                }
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(4);

        // 클래스 출력 폴더
        GUILayout.BeginHorizontal();
        GUILayout.Label("클래스 저장 폴더:", GUILayout.Width(100));
        string newClassFolder = GUILayout.TextField(_classOutputFolder);
        if (newClassFolder != _classOutputFolder)
        {
            _classOutputFolder = newClassFolder;
            EditorPrefs.SetString(PREFS_CLASS_FOLDER, _classOutputFolder);
        }
        if (GUILayout.Button("폴더 선택", GUILayout.Width(80)))
        {
            string folder = EditorUtility.OpenFolderPanel("클래스 저장 폴더 선택", Application.dataPath, "");
            if (!string.IsNullOrEmpty(folder))
            {
                if (folder.StartsWith(Application.dataPath))
                {
                    _classOutputFolder = "Assets" + folder.Substring(Application.dataPath.Length);
                    EditorPrefs.SetString(PREFS_CLASS_FOLDER, _classOutputFolder);
                }
                else
                {
                    UnityEngine.Debug.LogError("클래스 저장 폴더는 반드시 Assets 폴더 내부여야 합니다.");
                }
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(16);

        // ── 5) 생성 시작 버튼 ────────────────────────────────────────────
        if (GUILayout.Button("생성 시작", GUILayout.Height(40)))
        {
            if (!string.IsNullOrEmpty(_excelFolderPath))
            {
                // 폴더에서 .xlsx 파일 취합
                string[] filesInFolder = Directory.GetFiles(_excelFolderPath, "*.xlsx", SearchOption.TopDirectoryOnly);
                if (filesInFolder.Length == 0)
                {
                    UnityEngine.Debug.LogWarning($"폴더 '{_excelFolderPath}'에 .xlsx 파일이 없습니다.");
                    return;
                }
                ExecuteGenerate(filesInFolder);
            }
            else
            {
                if (_excelFilePaths.Count == 0)
                {
                    UnityEngine.Debug.LogError("엑셀 파일이 하나도 선택되지 않았습니다.");
                    return;
                }
                ExecuteGenerate(_excelFilePaths.ToArray());
            }
        }
    }

    // 드래그&드롭 이벤트 처리
    private void HandleDragAndDrop(Rect dropArea)
    {
        Event evt = Event.current;
        if (!dropArea.Contains(evt.mousePosition))
            return;

        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
                {
                    bool anyValid = false;
                    foreach (string path in DragAndDrop.paths)
                    {
                        if (Path.GetExtension(path).Equals(".xlsx", System.StringComparison.OrdinalIgnoreCase))
                        {
                            anyValid = true;
                            break;
                        }
                    }

                    DragAndDrop.visualMode = anyValid ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        foreach (string path in DragAndDrop.paths)
                        {
                            if (Path.GetExtension(path).Equals(".xlsx", System.StringComparison.OrdinalIgnoreCase))
                            {
                                if (!_excelFilePaths.Contains(path))
                                    _excelFilePaths.Add(path);
                            }
                        }
                    }
                }
                Event.current.Use();
                break;
        }
    }

    // 실제 GenerateFile 호출
    private static void ExecuteGenerate(string[] excelPaths)
    {
        EnsureFolderExists(_dataResourceOutputFolder);
        EnsureFolderExists(_classOutputFolder);

        Generator gen = new Generator();
        int processed = 0;
        try
        {
            foreach (var file in excelPaths)
            {
                gen.Generate(file, _dataResourceOutputFolder, _classOutputFolder);
                processed++;
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("완료", $"Excel → GameData/클래스 생성 완료\n총 {processed}개 파일 처리됨.", "확인");
            UnityEngine.Debug.Log("Excel → JSON/클래스 생성 완료");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"생성 중 오류 발생: {e}");
            EditorUtility.DisplayDialog("실패", $"Excel → GameData/클래스 생성 실패. 에디터 로그를 확인하세요.", "확인");
        }
    }

    // Assets 내부 폴더 없으면 생성
    private static void EnsureFolderExists(string assetsRelativePath)
    {
        if (!assetsRelativePath.StartsWith("Assets"))
            return;

        string full = Path.Combine(Application.dataPath, assetsRelativePath.Substring("Assets/".Length));
        if (!Directory.Exists(full))
        {
            UnityEngine.Debug.Log($"Create Directory : {full}");
            Directory.CreateDirectory(full);
        }
    }
}
