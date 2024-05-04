using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DevAccelerationSystem.ProjectCompilationCheck
{
    internal static class MenuItems
    {
        [MenuItem("Window/DevAccelerationSystem/ProjectCompilationCheck/Run all compilations", false, 1000)]
        public static void RunAllCompilations()
        {
            var output = ProjectCompiler.RunAll();
            Debug.Log("Compilation IsSuccess? "+ output.Results.Any(a => a.ErrorsCount < 1));
        }
        
        [MenuItem("Window/DevAccelerationSystem/ProjectCompilationCheck/Focus config", false, 1000)]
        public static void FocusConfig()
        {
            Selection.activeObject =ProjectCompilationConfigSO.Instance;
            Debug.Log("The config is actived at path "+ AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets(nameof(ProjectCompilationConfigSO))[0]));
        }
        
        [MenuItem("Window/DevAccelerationSystem/ProjectCompilationCheck/Focus viewer", false, 1000)]
        public static void FocusViewer()
        {
            Selection.activeObject = CompilationOutputViewerSO.Instance;
            Debug.Log("The config is actived at path "+AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets(nameof(CompilationOutputViewerSO))[0]));
        }
    }
}