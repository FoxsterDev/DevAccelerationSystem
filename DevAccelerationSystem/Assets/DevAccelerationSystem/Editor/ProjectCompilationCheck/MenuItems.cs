using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DevAccelerationSystem.ProjectCompilationCheck
{
    internal static class MenuItems
    {
        [MenuItem("Window/DevAccelerationSystem/ProjectCompilationCheck/Show Compilation Output Viewer", false, 3)]
        public static void ShowCompilationOutputViewerWindow()
        {
            EditorWindow.GetWindow(typeof(CompilationOutputViewerEditor));
        }
        [MenuItem("Window/DevAccelerationSystem/ProjectCompilationCheck/Show Compilation Output Viewer", true)]
        private static bool ValidateShowCompilationOutputViewerWindow()
        {
            return  ProjectCompilationConfigSO.Find() != null;
        }
        
        [MenuItem("Window/DevAccelerationSystem/ProjectCompilationCheck/Run all compilations", false, 1)]
        public static void RunAllCompilations()
        {
            var output = ProjectCompiler.RunAll();
            Debug.Log("Compilation IsSuccess? "+ output.Results.Any(a => a.ErrorsCount < 1));
        }
    
        [MenuItem("Window/DevAccelerationSystem/ProjectCompilationCheck/Run all compilations", true)]
        private static bool ValidateRunAllCompilations()
        {
            return  ProjectCompilationConfigSO.Find() != null;
        }
        
        [MenuItem("Window/DevAccelerationSystem/ProjectCompilationCheck/Focus config", true)]
        private static bool ValidateFocusConfig()
        {
            return  ProjectCompilationConfigSO.Find() != null;
        }

        [MenuItem("Window/DevAccelerationSystem/ProjectCompilationCheck/Focus config", false, 2)]
        public static void FocusConfig()
        {
            Selection.activeObject = ProjectCompilationConfigSO.Find();
        }
        
        [MenuItem("Window/DevAccelerationSystem/ProjectCompilationCheck/First create a project compilation config", true)]
        private static bool ValidateInstallConfig()
        {
            return  ProjectCompilationConfigSO.Find() == null;
        }

        [MenuItem("Window/DevAccelerationSystem/ProjectCompilationCheck/First create a project compilation config", false, 2)]
        public static void InstallConfig()
        {
           Application.OpenURL("https://github.com/FoxsterDev/DevAccelerationSystem?tab=readme-ov-file#creating-project-compilation-config");
        }
    }
}