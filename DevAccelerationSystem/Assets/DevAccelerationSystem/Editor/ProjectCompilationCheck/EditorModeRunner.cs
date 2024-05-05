using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEditor.Compilation;
using DevAccelerationSystem.Core;
using ILogger = DevAccelerationSystem.Core.ILogger;

[assembly: InternalsVisibleTo("FoxsterDevDebugger.Editor")]
namespace DevAccelerationSystem.ProjectCompilationCheck
{
    internal static class EditorModeRunner
    {
        public static CompilationOutput Run(CompilationConfig config, ILogger logger)
        {
            var st = new Stopwatch();
            st.Start();
            
            var nameStr = $"{nameof(EditorModeRunner)}:{nameof(Run)}:{config.Name}";
            logger.Info(nameStr);

            var compilationResults = RunProjectCompilation(logger, config);
            PostProjectCompilationCheckCall(logger, nameStr, compilationResults);
            
            st.Stop();
            return new CompilationOutput {
                Results = compilationResults, 
                Stats = new CompilationStats{ Name = nameStr, CompilationTotalMs = st.ElapsedMilliseconds},
                Logs = logger.ToString()
            };
        }

        public static CompilationOutput RunAll(List<CompilationConfig> compilationConfigs, ILogger logger)
        {
            var st = new Stopwatch();
            st.Start();
            
            var nameStr = $"{nameof(EditorModeRunner)}:{nameof(RunAll)}";
            logger.Info(nameStr);
            
            var compilationResults = RunProjectCompilation(logger, compilationConfigs.ToArray());
            
            PostProjectCompilationCheckCall(logger, nameStr, compilationResults);

            st.Stop();
        
            return new CompilationOutput {
                Results = compilationResults, 
                Stats = new CompilationStats{ Name = nameStr, CompilationTotalMs = st.ElapsedMilliseconds}, 
                Logs = logger.ToString()};
        }

        private static void PostProjectCompilationCheckCall(ILogger logger, string nameStr, List<CompilationResult> compilationResults)
        {
            EditorUtility.ClearProgressBar();

            logger.Info($"Project compilation {nameStr} is finished.");

            foreach (var result in compilationResults)
            {
                if (result.ErrorsCount > 0)
                {
                    logger.Error(
                        $"Compilation failed for {result.ProjectCompilationSettingName} with errors:\n{result.ErrorsList}");
                }
                else
                {
                    logger.Info($"Compilation succeded for {result.ProjectCompilationSettingName}");
                }
            }
        }

        private static List<CompilationResult> RunProjectCompilation(ILogger logger, params CompilationConfig[] compilationConfigs)
        {
            logger.Info($"{nameof(RunProjectCompilation)} with these compilation configs: {string.Join(";", compilationConfigs.Select(s=>s.Name))}");
            
            var compilationResults = new List<CompilationResult>();

            if (EditorApplication.isCompiling || 
                EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isUpdating)
            {
                logger.Error(
                    $"Unity editor is busy isCompiling: {EditorApplication.isCompiling}, " +
                    $"isPlayingOrWillChangePlaymode: {EditorApplication.isPlayingOrWillChangePlaymode}, " +
                    $"isUpdating: {EditorApplication.isUpdating}");
                return compilationResults;
            }
           
            foreach (var compilationConfig in compilationConfigs)
            {
                logger.Info($"Running compilation: {compilationConfig} ...");
                
                var output = new CompilationResult
                    {ProjectCompilationSettingName = compilationConfig.Name, CompilationConfig = compilationConfig};
                
                if (!compilationConfig.Enabled)
                {
                    logger.Warning($"The config {compilationConfig.Name} is disabled. Skip it.");
                    compilationResults.Add(output);
                    continue;
                }
                
                if(!compilationConfig.Target.IsBuildTargetSupported())
                {
                    logger.Warning($"The build target {compilationConfig.Target} is not supported. Skip it.");

                    output.ErrorsCount++;
                    output.ErrorsList += $"[{output.ErrorsCount}]: Unity module {compilationConfig.Target} is not installed. Try to install the module and restart the unity.\n";
                    compilationResults.Add(output);
                    continue;
                }
                
                var st = new Stopwatch();
                st.Start();
                    
                var filePath = CreateDirectoryIfNotExists(compilationConfig.Target);

                void CompilationFinished(string assemblyName, CompilerMessage[] compilerMessages)
                {
                    foreach (var m in compilerMessages)
                    {
                        switch (m.type)
                        {
                            case CompilerMessageType.Error:
                                output.ErrorsCount++;
                                output.ErrorsList += $"[{output.ErrorsCount}]: {m.message}\n";
                                break;
                        }
                    }
                }

                CompilationPipeline.assemblyCompilationFinished -= CompilationFinished;
                CompilationPipeline.assemblyCompilationFinished += CompilationFinished;
                output.UnityCompilationResult = PlayerBuildInterface.CompilePlayerScripts(compilationConfig, filePath);
                CompilationPipeline.assemblyCompilationFinished -= CompilationFinished;
                    
                st.Stop();
                output.CompilationStats = new CompilationStats{ Name= compilationConfig.Name, CompilationTotalMs = st.ElapsedMilliseconds};

                compilationResults.Add(output);
            }

            return compilationResults;
        }


        private static string CreateDirectoryIfNotExists(BuildTarget target)
        {
            var directoryPath = $"{Directory.GetCurrentDirectory()}/Library/ScriptAssemblies/PlayerBuildInterface/" +
                                target;
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, true);
            }

            Directory.CreateDirectory(directoryPath);

            return directoryPath;
        }
        
        
        /*public static CompilationOutput RunByName(string configName, ILogger logger = null)
        {
            logger ??= new DefaultUnityLogger(nameof(ProjectCompilationCheck), 40000);

            var nameStr = $"{nameof(EditorModeRunner)}:{nameof(RunByName)}:{configName}";
           
            
            ProjectCompilationConfigSO.ResetInstance();
            var so = ScriptableObjectLoader.LoadAllAssetsOfType<ProjectCompilationConfigSO>()[0];
            //  ProjectCompilationConfigSO.Instance;
            var compilationConfigs = so.CompilationConfigs;
           
            var config = compilationConfigs.Find(e => e.Name == configName);
            if (config == null)
            {
                logger.Error("Could not find a config with name: "+ configName);
                
                var result = new CompilationResult
                {   CompilationConfig = default, 
                    ErrorsCount = 1, 
                    ErrorsList = "Could not find a config with name: "+ configName, 
                    ProjectCompilationSettingName = configName};

                //st.Stop();
                return new CompilationOutput {Results = new List<CompilationResult>{result}, 
                    Stats = new CompilationStats{ Name = nameStr, CompilationTotalMs = 0}, Logs = logger.ToString()};
            }
            return Run(config, logger);
        }*/

    }
}