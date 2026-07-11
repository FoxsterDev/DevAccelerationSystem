using System;
using StabilityHub.Monitoring;
using UnityEngine;

namespace TheBestLogger.Examples
{
    public enum MonitoringConfigurationPreset
    {
        Production = 0,
        Qa = 1
    }

    public static class MonitoringConfigurationPresets
    {
        public static MonitoringConfiguration Create(MonitoringConfigurationPreset preset)
        {
            var configuration = ScriptableObject.CreateInstance<MonitoringConfiguration>();
            Apply(configuration, preset);
            return configuration;
        }

        public static MonitoringConfiguration CreateProduction()
        {
            return Create(MonitoringConfigurationPreset.Production);
        }

        public static MonitoringConfiguration CreateQa()
        {
            return Create(MonitoringConfigurationPreset.Qa);
        }

        public static void Apply(MonitoringConfiguration configuration, MonitoringConfigurationPreset preset)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            configuration.CrashReporterModule = preset switch
            {
                MonitoringConfigurationPreset.Production => new CrashReporterModuleConfiguration
                {
                    Enabled = true,
                    AutoProjectSettingsSetup = false,
                    IOS = new CrashReporteriOSConfiguration { Enabled = true }
                },
                MonitoringConfigurationPreset.Qa => new CrashReporterModuleConfiguration
                {
                    Enabled = true,
                    AutoProjectSettingsSetup = false,
                    IOS = new CrashReporteriOSConfiguration { Enabled = true }
                },
                _ => throw new ArgumentOutOfRangeException(nameof(preset), preset, null)
            };
        }
    }
}
