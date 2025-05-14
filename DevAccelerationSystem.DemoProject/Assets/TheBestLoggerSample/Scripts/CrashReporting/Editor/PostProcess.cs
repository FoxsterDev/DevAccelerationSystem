using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace TheBestLoggerSample.CrashReporting
{
    public class PostProcessBuild
    {
        [PostProcessBuild]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuildProject)
        {
            if (target == BuildTarget.iOS)
            {
                var projectPath = PBXProject.GetPBXProjectPath(pathToBuildProject);
                var project = new PBXProject();
                project.ReadFromFile(projectPath);

                var targetGuid = project.GetUnityMainTargetGuid();
                var targetGuid2 = project.GetUnityFrameworkTargetGuid();
                //return;
                // Enable Objective-C exceptions
                project.SetBuildProperty(targetGuid, "GCC_ENABLE_OBJC_EXCEPTIONS", "YES");
                project.SetBuildProperty(targetGuid2, "GCC_ENABLE_OBJC_EXCEPTIONS", "YES");
                // Save the modified project
                project.WriteToFile(projectPath);
            }
        }
    }
}
