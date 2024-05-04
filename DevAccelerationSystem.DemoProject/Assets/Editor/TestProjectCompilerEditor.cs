using System.Linq;
using UnityEditor;
using UnityEngine;

public class TestProjectCompilerEditor : Editor
{
    [MenuItem("Test/Run all compilations")]
    public static void Compile()
    {
        var output = DevAccelerationSystem.ProjectCompilationCheck.ProjectCompiler.RunAll();
        Debug.Log("Compilation IsSuccess? "+ output.Results.Any(a => a.ErrorsCount < 1));
    }
}