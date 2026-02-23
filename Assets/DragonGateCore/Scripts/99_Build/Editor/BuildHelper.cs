using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

namespace DragonGate
{
    public class BuildHelper : MonoBehaviour
    {
        private enum EBuildMode
        {
            Debug,
            Release
        }

        private const string buildFolderName = "Builds";

        private static string previousSymbols;

#if UNITY_ANDROID
    private static bool bIsAAB = true;

    [MenuItem("Build/Start Debug/apk")]
    public static void BuildAndroidDebugApk()
    {
        bIsAAB = false;
        BuildAndroidDebug();
    }

    [MenuItem("Build/Start Debug/aab")]
    public static void BuildAndroidDebugAab()
    {
        bIsAAB = true;
        BuildAndroidDebug();
    }

    private static void BuildAndroidDebug()
    {
        Build(EBuildMode.Debug);
    }

    [MenuItem("Build/Start Release/apk")]
    public static void BuildAndroidReleaseApk()
    {
        bIsAAB = false;
        BuildAndroidRelease();
    }
    
    [MenuItem("Build/Start Release/aab")]
    public static void BuildAndroidReleaseAab()
    {
        bIsAAB = true;
        BuildAndroidRelease();
    }

    private static void BuildAndroidRelease()
    {
        Build(EBuildMode.Release);
    }
#endif

#if UNITY_IOS
    [MenuItem("Build/Start Debug")]
    public static void BuildIOSDebug()
    {
        Build(EBuildMode.Debug);
    }
    [MenuItem("Build/Start Release")]
    public static void BuildIOSRelease()
    {
        Build(EBuildMode.Release);
    }
#endif

#if UNITY_STANDALONE_WIN
        [MenuItem("Build/Start Debug")]
        public static void BuildWinDebug()
        {
            Build(EBuildMode.Debug);
        }

        [MenuItem("Build/Start Release")]
        public static void BuildWinRelease()
        {
            Build(EBuildMode.Release);
        }
#endif

#if UNITY_STANDALONE_OSX
        [MenuItem("Build/Start Debug")]
        public static void BuildMacDebug()
        {
            Build(EBuildMode.Debug);
        }

        [MenuItem("Build/Start Release")]
        public static void BuildMacRelease()
        {
            Build(EBuildMode.Release);
        }
#endif

        private static void Build(EBuildMode mode, bool cleanBuild = false)
        {
            UnityEngine.Debug.Log($"[DGBuild] Build Started : Mode - {mode.ToString()}");
            previousSymbols = DefineSymbolManager.GetSymbols();
            if (mode == EBuildMode.Debug)
            {
                DevModeSwitcher.SetDebugMode();
            }
            else if (mode == EBuildMode.Release)
            {
                DevModeSwitcher.SetReleaseMode();
            }

            var option = new BuildPlayerOptions();

            var includeSceneList = ListPool<string>.Get();
            var buildSettingScenes = EditorBuildSettings.scenes;
            foreach (var scene in buildSettingScenes)
            {
                if (scene.enabled)
                {
                    includeSceneList.Add(scene.path);
                }
            }
            option.scenes = includeSceneList.ToArray();
            ListPool<string>.Release(includeSceneList);

            var appName = mode == EBuildMode.Debug ? $"{Application.productName}_{mode.ToString()}" : Application.productName;

            var developmentBuild = mode == EBuildMode.Debug;
            EditorUserBuildSettings.development = developmentBuild;
            EditorUserBuildSettings.allowDebugging = developmentBuild;
            EditorUserBuildSettings.connectProfiler = developmentBuild;
            
#if UNITY_ANDROID
        if (bIsAAB)
            option.locationPathName = $"{buildFolderName}/{appName}.aab";
        else
            option.locationPathName = $"{buildFolderName}/{appName}.apk";
        
        EditorUserBuildSettings.buildAppBundle = bIsAAB;
#elif UNITY_IOS
        option.locationPathName = $"{buildFolderName}/{appName}";
#elif UNITY_STANDALONE_WIN
            option.locationPathName = $"{buildFolderName}/{appName}.exe";
#elif UNITY_STANDALONE_OSX
        option.locationPathName = $"{buildFolderName}/{appName}.app";
#else
		Debug.LogError("Build location path is not set.");
		return;
#endif
            option.target = EditorUserBuildSettings.activeBuildTarget;
            option.options = BuildOptions.None;
            
            if (cleanBuild && Directory.Exists(buildFolderName))
            {
                Directory.Delete(buildFolderName, true);
            }

            var report = BuildPipeline.BuildPlayer(option);

            if (previousSymbols != null)
                DefineSymbolManager.SetSymbols(previousSymbols);

            UnityEngine.Debug.Log($"[DGBuild] Build completed : {report.summary.result.ToString()}");
        }
    }
}