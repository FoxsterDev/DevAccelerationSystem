using System;
using System.Diagnostics;
using System.Reflection;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR

namespace TheBestLogger
{
    public static class ReflectiveUnityEditorConsoleLogger
    {
        private static readonly Lazy<MethodInfo> _addMessageMethod = new(InitializeReflectionComponents);
        private static Type _logEntryType;

#if THEBESTLOGGER_DIAGNOSTICS_ENABLED
        [MenuItem("Tools/TheBestLogger/Log Direct To Console")]
        private static void TestLogDirect()
        {
            LogToConsoleDirectly("User X performed action Y.\nDetails: timestamp, item_id, status_code.");
        }
#endif
        private static MethodInfo InitializeReflectionComponents()
        {
            var editorAssembly = Assembly.GetAssembly(typeof(Editor));
            if (editorAssembly == null)
            {
                return null;
            }

            var logEntriesType = editorAssembly.GetType("UnityEditor.LogEntries");
            _logEntryType = editorAssembly.GetType("UnityEditor.LogEntry");

            if (logEntriesType == null || _logEntryType == null)
            {
                return null;
            }

            var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var paramTypes = new[] { _logEntryType };

            return logEntriesType.GetMethod("AddMessageWithDoubleClickCallback", flags, null, paramTypes, null);
        }

        [Conditional("UNITY_EDITOR")]
        public static void LogToConsoleDirectly(string message, LogType type = LogType.Log)
        {
            var addMessageMethod = _addMessageMethod.Value;

            if (addMessageMethod == null || _logEntryType == null)
            {
                return;
            }

            try
            {
                var logEntryInstance = Activator.CreateInstance(_logEntryType);

                _logEntryType.GetField("message", BindingFlags.Public | BindingFlags.Instance)?.SetValue(logEntryInstance, message);

                var modeField = _logEntryType.GetField("mode", BindingFlags.Public | BindingFlags.Instance);
                if (modeField != null)
                {
                    var modeValue = 0; //0 nothing , 1 error, 2 error, 3 error, 4 info , 5 error, 8 nothing 16 fatal popup
                    //6 error, 7 error , 9 error

                    switch (type)
                    {
                        case LogType.Log: modeValue = 4; break; // Ваши специфичные значения
                        //case LogType.Warning:   modeValue = 2; break;
                        case LogType.Error: modeValue = 1; break;
                        case LogType.Assert: modeValue = 1; break;
                        case LogType.Exception: modeValue = 1; break;
                        default: modeValue = 4; break;
                    }

                    modeField.SetValue(logEntryInstance, modeValue);
                }

                addMessageMethod.Invoke(null, new[] { logEntryInstance });
            }
#if THEBESTLOGGER_DIAGNOSTICS_ENABLED
            catch (Exception ex)
            {
                Diagnostics.Write("ReflectiveUnityEditorConsoleLogger has issues: " + ex.Message + "", LogLevel.Exception, ex);
            }
#else
            catch (Exception)
            {
                //ignore
            }
#endif
        }
    }
}
#endif
