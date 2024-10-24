using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using NUnit.Framework;
using TheBestLogger.Examples.LogTargets;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace TheBestLogger.Integration.Tests
{
    public class LogTargetConfigurationBackCompabilityTests
    {
        private static List<(string logTargetConfigurationName, string assetPath, string json)> GetListLogTargetConfigurationJsons()
        {
            var list = new List<(string logTargetConfigurationName, string assetPath, string json)>(1);
            var filterAssetId = nameof(LogTargetConfigurationBackCompabilityTests);
            var guid = AssetDatabase.FindAssets(filterAssetId)[0];
            var path = AssetDatabase.GUIDToAssetPath(guid).Replace(filterAssetId + ".cs", "");
            var testsDataAssetPath = AssetDatabase.FindAssets("t:TextAsset", new[] { path });
            foreach (string jsonGuid in testsDataAssetPath)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(jsonGuid);
                if (!assetPath.Contains(".json")) continue;

                var logTargetConfigurationName = assetPath.Split('/').First(e => e.Contains("LogTargetConfiguration"));

                var jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);

                if (jsonAsset != null)
                {
                    list.Add(new(logTargetConfigurationName, assetPath, jsonAsset.text));
                }
            }

            return list;
        }
        public static void VerifyFieldsNotNull(object obj)
        {
            foreach (var field in obj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                object value = field.GetValue(obj);
                if (value == null)
                {
                    throw new InvalidOperationException($"Field '{field.Name}' is null");
                }
            }
        }
        [Test]
        [TestCaseSource(nameof(GetTestData))]
        public void ThePreviousVersionCanBeDeserializedWithNewtonJson(string logTargetConfigurationName, string json)
        {
            if (logTargetConfigurationName == nameof(OpenSearchLogTargetConfiguration))
            {
                Assert.DoesNotThrow(
                    () =>
                    {
                        var obj = JsonConvert.DeserializeObject<OpenSearchLogTargetConfiguration_1_0_0>(json);
                        VerifyFieldsNotNull(obj);
                        if (obj == null ||
                            obj.DebugMode == null ||
                            obj.BatchLogs.MaxCountLogs == 0)
                        {
                            throw new Exception(logTargetConfigurationName + " is not valid");
                        }
                    });
            }
        }

        [Test]
        [TestCaseSource(nameof(GetTestData))]
        public void TheCurrentVersionCanBeDeserializedWithNewtonJson(string logTargetConfigurationName, string json)
        {
            if (logTargetConfigurationName == nameof(OpenSearchLogTargetConfiguration))
            {
                Assert.DoesNotThrow(
                    () =>
                    {
                        var obj = JsonConvert.DeserializeObject<OpenSearchLogTargetConfiguration>(json);
                        VerifyFieldsNotNull(obj);
                        if (obj == null ||
                            obj.DebugMode == null ||
                            obj.BatchLogs.MaxCountLogs == 0 || obj.StackTraces == null || obj.StackTraces.Length < 1)
                        {
                            throw new Exception(logTargetConfigurationName + " is not valid");
                        }
                    });
            }
        }

        [Test]
        [TestCaseSource(nameof(GetTestData))]
        public void TheCurrentVersionCanBeMergedWithDeserializedVersionByNewton(string logTargetConfigurationName, string json)
        {
            if (logTargetConfigurationName == nameof(OpenSearchLogTargetConfiguration))
            {
                var current = new OpenSearchLogTargetConfiguration();
                current.StackTraces[(int) LogLevel.Exception].Enabled = false;
                current.StackTraces[(int) LogLevel.Info].Enabled = true;
                current.StackTraces[(int) LogLevel.Warning].Enabled = true;
                Assert.DoesNotThrow(
                    () =>
                    {
                        var obj = JsonConvert.DeserializeObject<OpenSearchLogTargetConfiguration>(json);
                        if (obj == null ||
                            obj.DebugMode == null ||
                            obj.BatchLogs.MaxCountLogs == 0 || obj.StackTraces == null || obj.StackTraces.Length < 1)
                        {
                            throw new Exception(logTargetConfigurationName + " is not valid");
                        }

                        current.Merge(obj);
                    });

                Assert.IsFalse(current.StackTraces[(int) LogLevel.Warning].Enabled);
                Assert.IsFalse(current.StackTraces[(int) LogLevel.Info].Enabled);
                Assert.IsTrue(current.StackTraces[(int) LogLevel.Exception].Enabled);
            }
        }

        [Test]
        public void TheCurrentVersionCanBeSerializableWithNewtonJson()
        {
            var obj = new OpenSearchLogTargetConfiguration();
            Assert.DoesNotThrow(
                () =>
                {
                    var json = JsonConvert.SerializeObject(obj);
                    Debug.Log(json);
                    if (json == null || string.IsNullOrWhiteSpace(json) || json == "{}")
                    {
                        throw new Exception(obj + " is not valid");
                    }
                });
        }

        [Test]
        public void TheCurrentVersionCanBeSerializableWithUnityJson()
        {
            var obj = new OpenSearchLogTargetConfiguration();
            Assert.DoesNotThrow(
                () =>
                {
                    var json = JsonUtility.ToJson(obj);
                    if (json == null || string.IsNullOrWhiteSpace(json) || json == "{}")
                    {
                        throw new Exception(obj + " is not valid");
                    }
                });
        }

        [Test]
        [TestCaseSource(nameof(GetTestData))]
        public void ThePreviousVersionCanBeDeserializedWithUnityJson(string logTargetConfigurationName, string json)
        {
            if (logTargetConfigurationName == nameof(OpenSearchLogTargetConfiguration))
            {
                Assert.DoesNotThrow(
                    () =>
                    {
                        var obj = JsonUtility.FromJson<OpenSearchLogTargetConfiguration_1_0_0>(json);
                        VerifyFieldsNotNull(obj);
                        if (obj == null ||
                            obj.DebugMode == null ||
                            obj.BatchLogs.MaxCountLogs == 0)
                        {
                            throw new Exception(logTargetConfigurationName + " is not valid");
                        }
                    });
            }
        }

        private static IEnumerable GetTestData()
        {
            var list = GetListLogTargetConfigurationJsons();
            foreach (var e in list)
            {
                if (e.json != null)
                {
                    yield return new TestCaseData(e.logTargetConfigurationName, e.json).SetName(
                        $"Test_{Path.GetFileNameWithoutExtension(e.assetPath)}_Input{e.logTargetConfigurationName}_ExpectedToBeCompatible");
                }
                else
                {
                    Assert.Fail("JSON asset not found at: " + e.assetPath);
                }
            }
        }
    }
}
