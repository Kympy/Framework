#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class AutoIncrementBundleVersionCode : IPreprocessBuildWithReport
{
    public int callbackOrder => 0; // 빌드 시작 전에 실행

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform == BuildTarget.Android)
        {
            int currentVersionCode = PlayerSettings.Android.bundleVersionCode;
            int newVersionCode = currentVersionCode + 1;

            PlayerSettings.Android.bundleVersionCode = newVersionCode;

            Debug.Log($"[AutoIncrement] Bundle Version Code 증가: {currentVersionCode} → {newVersionCode}");

            // 변경 사항 저장
            AssetDatabase.SaveAssets();
        }
    }
}
#endif