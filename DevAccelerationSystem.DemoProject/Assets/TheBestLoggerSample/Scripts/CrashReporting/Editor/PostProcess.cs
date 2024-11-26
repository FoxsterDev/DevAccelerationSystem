using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

public class PostProcessBuild
{
    [PostProcessBuild]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuildProject)
    {
        if (target == BuildTarget.iOS)
        {
            string projectPath = PBXProject.GetPBXProjectPath(pathToBuildProject);
            PBXProject project = new PBXProject();
            project.ReadFromFile(projectPath);

            string targetGuid = project.GetUnityMainTargetGuid();
            string targetGuid2 = project.GetUnityFrameworkTargetGuid();
            //return;
            // Enable Objective-C exceptions
            project.SetBuildProperty(targetGuid, "GCC_ENABLE_OBJC_EXCEPTIONS", "YES");
            project.SetBuildProperty(targetGuid2, "GCC_ENABLE_OBJC_EXCEPTIONS", "YES");
            // Save the modified project
            project.WriteToFile(projectPath);
        }
    }
}