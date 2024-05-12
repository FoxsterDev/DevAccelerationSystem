using System.Collections.Generic;
using DevAccelerationSystem.Core;
using NUnit.Framework;
using UnityEngine;

namespace DevAccelerationSystem.Tests.Editor
{
    [Category("DevAccelerationSystem")]
    public class CommandLineArgumentsTests
    {
        private static string[] TestCaseSourcesWhenNotValidLineCommandArguments =
        {
            "",
            "-",
            "-batchmode -nographics",
        };
        
        private static string[] TestCaseSourcesWhenValidLineCommandArguments =
        {
            "/Applications/Unity/2021.3.13f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics",

            "/Applications/Unity/2022.3.13f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -ignorecompileerrors -silent-crashes -force-free " +
            "-projectPath /Users/Projects/Sandbox/DevAccelerationSystem/DevAccelerationSystem.DemoProject -executeMethod DevAccelerationSystem.ProjectCompilationCheck.BatchModeRunner.Run -configName -compilationOutput /Users/Projects/Sandbox/DevAccelerationSystem/Output",

        };

        private static List<string[]> TestCaseSourcesWhenValidCommandArguments
        {
            get
            {
                return new List<string[]>
                {
                    new[]
                    {
                        "/Applications/Unity/2021.3.13f1/Unity.app/Contents/MacOS/Unity",
                        "-batchmode",
                        "-quit",
                        "-projectPath",
                        "/Users/Projects/Sandbox/DevAccelerationSystem/DevAccelerationSystem.DemoProject",
                        "-executeMethod",
                        "DevAccelerationSystem.ProjectCompilationCheck.BatchModeRunner.Run",
                        "-logFile",
                        "/dev/stdout"
                    }
                };
            }
        }

        [Test]
        [TestCaseSource(nameof(TestCaseSourcesWhenNotValidLineCommandArguments))]
        public void CommandArguments_ParseStringWithWrongExecutableFile_IsNotValid(string caseSource)
        {
            var commandLineArguments = new CommandLineArgsParser(caseSource);
            Assert.False(commandLineArguments.IsValid);
        }
        
        [Test]
        [TestCaseSource(nameof(TestCaseSourcesWhenValidLineCommandArguments))]
        public void CommandArguments_ParseString_IsValid(string caseSource)
        {
            var commandLineArguments = new CommandLineArgsParser(caseSource);
            Assert.True(commandLineArguments.IsValid);
        }

        [Test]
        [TestCaseSource(nameof(TestCaseSourcesWhenValidCommandArguments))]
        public void CommandArguments_ParseArrayOfString_IsValid(string[] caseSource)
        {
            var commandLineArguments = new CommandLineArgsParser(caseSource);
            Assert.True(commandLineArguments.IsValid);
        }
    }
}