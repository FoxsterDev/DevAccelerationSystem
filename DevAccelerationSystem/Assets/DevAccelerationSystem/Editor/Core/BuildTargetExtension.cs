using System.Runtime.CompilerServices;
using UnityEditor;

[assembly: InternalsVisibleTo("DevAccelerationSystem.Editor.Tests")]
namespace DevAccelerationSystem.ProjectCompilationCheck
{
    public static class BuildTargetExtension
    {
        public static bool IsBuildTargetSupported(this BuildTarget target)
        {
            try
            {
                return BuildPipeline.IsBuildTargetSupported(target.ConvertToBuildTargetGroup(), target);
            }
            catch
            {
                return false;
            }
        }

        public static BuildTargetGroup ConvertToBuildTargetGroup(this BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.Android: return BuildTargetGroup.Android;
                case BuildTarget.iOS: return BuildTargetGroup.iOS;
                case BuildTarget.WebGL: return BuildTargetGroup.WebGL;
                case BuildTarget.WSAPlayer: return BuildTargetGroup.WSA;
                case BuildTarget.StandaloneWindows: return BuildTargetGroup.Standalone;
                case BuildTarget.StandaloneWindows64: return BuildTargetGroup.Standalone;
                case BuildTarget.StandaloneLinux64: return BuildTargetGroup.Standalone;
                case BuildTarget.StandaloneOSX: return BuildTargetGroup.Standalone;
                #if !UNITY_2023_1_OR_NEWER
                case BuildTarget.Stadia: return BuildTargetGroup.Stadia;
                #endif
                case BuildTarget.tvOS: return BuildTargetGroup.tvOS;
                case BuildTarget.Switch: return BuildTargetGroup.Switch;

                case BuildTarget.PS5: return BuildTargetGroup.PS5;
                case BuildTarget.PS4: return BuildTargetGroup.PS4;


                case BuildTarget.NoTarget: return BuildTargetGroup.Unknown;
            }

            return BuildTargetGroup.Unknown;
        }
    }
}
