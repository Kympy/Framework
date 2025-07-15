#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Framework
{
    public class DefineSymbolManager
    {
        public static string GetSymbols()
        {
            BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            NamedBuildTarget target = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);

            return PlayerSettings.GetScriptingDefineSymbols(target);
        }

        public static void SetSymbols(string symbols)
        {
            BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            NamedBuildTarget target = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            PlayerSettings.SetScriptingDefineSymbols(target, symbols);
        }

        public static void AddSymbol(string symbol)
        {
            BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            NamedBuildTarget target = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);

            string previousSymbols = PlayerSettings.GetScriptingDefineSymbols(target);
            if (previousSymbols.IndexOf(symbol, StringComparison.Ordinal) != -1)
            {
                Debug.Log($"[DefineSymbolManager] Define symbol '{symbol}' is already exists.");
                return;
            }

            string newSymbols = $"{previousSymbols};{symbol};";
            PlayerSettings.SetScriptingDefineSymbols(target, newSymbols);
            Debug.Log($"[DefineSymbolManager] Define symbol '{symbol}' is added.");
        }

        public static void RemoveSymbol(string symbol)
        {
            BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            NamedBuildTarget target = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);

            string previousSymbols = PlayerSettings.GetScriptingDefineSymbols(target);
            int index = previousSymbols.IndexOf(symbol, StringComparison.Ordinal);
            if (index == -1)
            {
                Debug.Log($"[DefineSymbolManager] Define symbol '{symbol}' is not exists.");
                return;
            }

            string newSymbols = previousSymbols.Remove(index, $"{symbol};".Length);
            PlayerSettings.SetScriptingDefineSymbols(target, newSymbols);
        }
    }
}
#endif
