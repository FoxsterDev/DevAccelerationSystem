using System.Collections.Generic;
using System.Linq;
using DevAccelerationSystem.Core;

namespace DevAccelerationSystem.ProjectCompilationCheck
{
    /// <summary>
    /// This API is used to interact with the DevAccelerationSystem ProjectCompilationCheck system.
    /// </summary>
    public static class ProjectCompiler
    {
        /// <summary>
        /// Adds a new compilation configuration. If a configuration with the same name exists, it returns false.
        /// </summary>
        /// <param name="name">A unique name for the configuration.</param>
        /// <param name="settings">A ProjectCompilationConfig containing various settings such as define symbols, target platforms, etc.</param>
        /// <returns>Returns a bool indicating success or failure.</returns>
        public static bool CreateConfiguration(string name, CompilationConfig settings)
        {
            var so = ProjectCompilationConfigSO.Find();
            var index = so.CompilationConfigs.FindIndex(c => c.Name == name);
            if (index > -1)
            {
                return false;
            }
            
            so.CompilationConfigs.Add(settings);
            so.SaveChanges(true);
            return false;
        }

        /// <summary>
        /// Removes a specified configuration.
        /// </summary>
        /// <param name="name">The name of the configuration to delete.</param>
        /// <returns>Returns a bool indicating if the deletion was successful.</returns>
        public static bool DeleteConfiguration(string name)
        {
            var so = ProjectCompilationConfigSO.Find();
            var index = so.CompilationConfigs.FindIndex(c => c.Name == name);
            if (index > -1)
            {
                so.CompilationConfigs.RemoveAt(index);
                so.SaveChanges(true);
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Modifies an existing configuration.
        /// </summary>
        /// <param name="name">The name of the configuration to update.</param>
        /// <param name="settings">Updated ProjectCompilationConfig.</param>
        /// <returns>Returns a bool indicating if the update was successful.</returns>
        public static bool UpdateConfiguration(string name, CompilationConfig settings)
        {
            var so = ProjectCompilationConfigSO.Find();
            var index = so.CompilationConfigs.FindIndex(c => c.Name == name);
            if (index > -1)
            {
                so.CompilationConfigs[index] = settings;
                so.SaveChanges(true);
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Retrieves the settings for a specific configuration.
        /// </summary>
        /// <param name="name">The name of the configuration to retrieve.</param>
        /// <returns>Returns a ProjectCompilationConfig representing the configuration settings.</returns>
        public static CompilationConfig GetConfiguration(string name)  
        {
            return ProjectCompilationConfigSO.Find()?.CompilationConfigs.Find(c => c.Name == name);
        }
        
        /// <summary>
        /// Provides a list of all existing configuration names.
        /// </summary>
        /// <returns>Returns a List of string containing the names of all configurations.</returns>
        public static List<string> ListConfigurations()  
        {
            return ProjectCompilationConfigSO.Find()?.CompilationConfigs.Select(c => c.Name).ToList();
        }

        /// <summary>
        /// Executes the compilation using the specified configuration.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        /// <returns>Returns a CompilationResult object that includes details like success, errors</returns>
        public static CompilationOutput Run(CompilationConfig config, ILogger logger = default)
        {
            logger ??= new DefaultUnityLogger(nameof(ProjectCompilationCheck), 40000);
            return EditorModeRunner.Run(config, logger);
        }

        /// <summary>
        /// Executes all stored configurations
        /// </summary>
        /// <returns>Returns a List of CompilationResult with results for each configuration.</returns>
        public static CompilationOutput RunAll(ILogger logger = default)
        {
            logger ??= new DefaultUnityLogger(nameof(ProjectCompilationCheck), 40000);
            var so = ProjectCompilationConfigSO.Find(logger);
            if (so == null)
            {
                return null;
            }
            return EditorModeRunner.RunAll(so.CompilationConfigs, logger);
        }
    }
}