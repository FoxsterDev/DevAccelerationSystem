using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using DevAccelerationSystem.Core;
using ILogger = DevAccelerationSystem.Core.ILogger;

namespace DevAccelerationSystem.ProjectCompilationCheck
{
    internal static class BatchModeRunner
    {
        internal static void Run()
        {
            var logger = (ILogger) new DefaultUnityLogger(nameof(ProjectCompilationCheck), 30000);
            
            var args = new CommandLineArgsParser();
            if (!args.IsValid)
            {
                logger.Error("Invalid command line arguments");
                EditorApplication.Exit(1);
                return;
            }
            
            var configSo = ProjectCompilationConfigSO.Find(logger);
            if (configSo == null)
            {
                EditorApplication.Exit(1);
                return;
            }
            
            CompilationOutput output = null;
            var configName = args["-configName"];
            if (!string.IsNullOrEmpty(configName) && configName != "RunAll")
            {
                var compilationConfig = configSo.CompilationConfigs.Find(e => e.Name == configName);
                if (compilationConfig == null)
                {
                    logger.Error("Could not find a config with name: " + configName);
                    EditorApplication.Exit(1);
                    return;
                }

                output = EditorModeRunner.Run(compilationConfig, logger);
            }
            else 
            {
                output = EditorModeRunner.RunAll(configSo.CompilationConfigs, logger);
            }
            
            if (!string.IsNullOrEmpty(args["-compilationOutput"]))
            {
                FileUtility.SaveAsJson(output, args["-compilationOutput"], false, false);
            }

            var exitCode = output.Results.Any(i => i.ErrorsCount > 0) ? 1 : 0;
            EditorApplication.Exit(exitCode);
        }
    }
}