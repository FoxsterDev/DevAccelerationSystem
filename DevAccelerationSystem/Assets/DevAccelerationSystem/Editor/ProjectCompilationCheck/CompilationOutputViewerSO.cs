using DevAccelerationSystem.Core;

namespace DevAccelerationSystem.ProjectCompilationCheck
{
    [AssetPath(nameof(ProjectCompilationCheck) + "SO/" + nameof(CompilationOutputViewerSO) + ".asset",
        AssetPathAttribute.Location.LocallyInParentFolderWithTheScriptType, typeof(CompilationOutputViewerSO),
        nameof(DevAccelerationSystem))]
    internal sealed class CompilationOutputViewerSO : SOSingleton<CompilationOutputViewerSO>
    {
       
    }
}