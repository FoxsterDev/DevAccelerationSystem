using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DevAccelerationSystem.ProjectCompilationCheck;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace DevAccelerationSystem.Tests.Editor
{
    [Category("DevAccelerationSystem")]
    public class BuildTargetExtensionsTests
    {
       // EditorUserBuildSettings.activeBuildTarget
        [Test]
        public void IsPlatformSupportLoadedByBuildTarget_GetMethodViaReflection_IsAvailable()
        {
            //arrange
            MethodInfo methodInfo = null;
            Action act = () => methodInfo = BuildTargetExtension.IsPlatformSupportLoadedByBuildTargetGetMethod();
            //act
            act.Invoke();
            //assert
            Assert.IsNotNull(methodInfo, $"Could not get the method in the Unity version. Reflection of extracting has to be updated in the Unity verion {Application.unityVersion}");
        }

    }
}