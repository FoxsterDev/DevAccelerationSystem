using System;
using DevAccelerationSystem.ProjectCompilationCheck;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DevAccelerationSystem.Tests.Editor
{
    [Category("DevAccelerationSystem")]
    public class BuildTargetExtensionsTests
    {
       // 
       [Test]
       public void IsCurrentPlatform_IsBuildTargetSupported_IsSupported()
       {
           //arrange
           Func<bool> act = () => BuildTargetExtension.IsBuildTargetSupported(EditorUserBuildSettings.activeBuildTarget);
           //act
           var isSupported = act.Invoke();
           //assert
           Assert.IsTrue(isSupported, $"The active build target must be available in Unity {Application.unityVersion}.");
       }
    }
}
