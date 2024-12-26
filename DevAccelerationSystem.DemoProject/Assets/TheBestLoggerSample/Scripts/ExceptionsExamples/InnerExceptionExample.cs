using System;
using System.IO;
using UnityEngine;

namespace TheBestLogger.Examples
{
    public class InnerExceptionExample 
    {
        public void ThrowHandledException()
        {
            try
            {
                LoadConfiguration();
            }
            catch (Exception ex)
            {
                GameLogger.Main.LogException(ex, new LogAttributes(LogImportance.Critical));
            }
        }
    
        private void LoadConfiguration()
        {
            try
            {
                // Attempt to read configuration from disk
                string configFilePath = Application.dataPath + "/config.json";
                if (!File.Exists(configFilePath))
                {
                    throw new FileNotFoundException("Configuration file not found on disk.");
                }
    
                string configContent = File.ReadAllText(configFilePath);
                Debug.Log("Configuration loaded from disk.");
            }
            catch (Exception fileException)
            {
                // If reading from disk fails, try to download it
                try
                {
                    DownloadConfiguration();
                }
                catch (Exception downloadException)
                {
                    // If downloading also fails, wrap the original exception inside a new one
                    throw new Exception("Failed to load configuration from both disk and online source.", fileException)
                    {
                        Source = downloadException.Message // Attach download error details
                    };
                }
            }
        }
    
        private static void DownloadConfiguration()
        {
            // Simulate a failure in downloading the configuration
            throw new InvalidOperationException("Failed to download configuration from server.");
        }
    }
}
