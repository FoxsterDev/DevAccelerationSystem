using System.Linq;
using UnityEditor;
using DevAccelerationSystem.Core;

namespace DevAccelerationSystem.ProjectCompilationCheck
{
    internal static class BatchModeRunner
    {
        internal static void Run()
        {
            var logger = (ILogger) new DefaultUnityLogger(nameof(ProjectCompilationCheck), 40000);
            
            var args = new CommandLineArgsParser();
            if (!args.IsValid)
            {
                logger.Error("Invalid command line arguments");
                EditorApplication.Exit(1);
                return;
            }
            
            CompilationOutput output = null;
            if (!string.IsNullOrEmpty(args["-configName"]) && args["-configName"] != "RunAll")
            {
                output = EditorModeRunner.RunByName(args["-configName"], logger);
            }
            else 
            {
                output = EditorModeRunner.RunAll(logger);
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