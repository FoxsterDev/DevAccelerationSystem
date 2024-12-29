using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace TheBestLoggerSample.CrashReporting
{
    public class CrashMe : MonoBehaviour
    {
        // Delegate that represents the Unity method to call
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void UnityCallback();

        [SerializeField]
        private ScrollButton _buttonPrefab;

        [SerializeField]
        private Transform _contentNativeExceptions;

        [SerializeField]
        private Text _consoleLabel;

        [SerializeField]
        private GameObject _nativeButtonActionsView;

        [SerializeField]
        private GameObject _scrollView;

        [SerializeField]
        private GameObject _crashAPIView;

        private SynchronizationContext _context;

        public void Start()
        {
            _context = SynchronizationContext.Current;
            
            /*Application.logMessageReceivedThreaded += (string condition,
                                                       string stacktrace,
                                                       LogType type) =>
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                {
                    _context.Post((s) => { _consoleLabel.text += $"{type}:" + condition; }, null);
                }
            };*/
            var list = ListTriggerMethods();
            // Log the methods to the Unity console
            if (list.Any())
            {
                //Debug.Log("Trigger Methods in NativeExceptionsiOS:");
                foreach (var method in list)
                {
                    //Debug.Log(method.Name);
                    var clone = Instantiate(_buttonPrefab, _contentNativeExceptions);
                    clone._label.text = method.Name;
                    clone.name = method.Name + "Button";
                    clone.gameObject.SetActive(true);
                    clone.GetComponent<Button>().onClick.AddListener(
                        () =>
                        {
                            Debug.Log("Calling " + method.Name);
                            method.Invoke(null, null);
                        });
                }
            }
            else
            {
                Debug.Log("No methods with 'Trigger' prefix found.");
            }
        }

        public void OnButtonClickNativeActionsTab()
        {
            if (!_scrollView.activeSelf)
            {
                _nativeButtonActionsView.SetActive(true);
                _scrollView.SetActive(true);
                _crashAPIView.SetActive(false);
            }
            else
            {
                _nativeButtonActionsView.SetActive(!_nativeButtonActionsView.activeSelf);
            }
        }

        public void OnButtonClickCrashAPITab()
        {
            _crashAPIView.SetActive(true);
            _scrollView.SetActive(false);
        }

        public static List<MethodInfo> ListTriggerMethods()
        {
            // Get the type of the class
            var type = typeof(NativeExceptionsiOS);

            // Get all static methods with the "Trigger" prefix
            var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public)
                              .Where(m => m.Name.StartsWith("Trigger"))
                              .ToList();

            return methods;
        }

        // Import the native method from the iOS plugin
        [DllImport("__Internal")]
        private static extern void RunOnBackgroundThread(IntPtr callback);

        // Public method to wrap a Unity action in dispatch_async
        public static void ExecuteOnBackgroundThread(Action action)
        {
            // Ensure the action is not null
            if (action == null)
            {
                Debug.LogError("Action passed to ExecuteOnBackgroundThread cannot be null.");
                return;
            }

            // Create a delegate that wraps the action
            UnityCallback callback = new UnityCallback(() => action());

            // Marshal the delegate to a function pointer
            IntPtr callbackPtr = Marshal.GetFunctionPointerForDelegate(callback);

            // Call the native method
            RunOnBackgroundThread(callbackPtr);
        }

        /*
         *  // Run a simple action on a background thread
                  NativeDispatcher.ExecuteOnBackgroundThread(() =>
                  {
                      Debug.Log($"This is running on a background thread! Thread ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
                  });
         */
    }
}
