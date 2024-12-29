using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AOT;
using UnityEngine;

namespace TheBestLoggerSample.CrashReporting
{
    public class NativeExceptionsiOS
    {
        [DllImport("__Internal")]
        private static extern void CauseNullPointerCrash();

        [DllImport("__Internal")]
        private static extern void CauseArrayBoundsCrash();

        [DllImport("__Internal")]
        private static extern void CauseDivideByZeroCrash();

        [DllImport("__Internal")]
        private static extern void CauseUnalignedMemoryAccessCrash();

        [DllImport("__Internal")]
        private static extern void CauseStackOverflowCrash();

        [DllImport("__Internal")]
        private static extern void CauseDoubleFreeCrash();

        [DllImport("__Internal")]
        private static extern void CauseUseAfterFreeCrash();

        [DllImport("__Internal")]
        private static extern void CauseIllegalInstructionCrash();

        [DllImport("__Internal")]
        private static extern void CauseInvalidMemoryAccessCrash();

        [DllImport("__Internal")]
        private static extern void CauseUncaughtExceptionCrash();

        [DllImport("__Internal")]
        private static extern void CauseInvalidFunctionPointerCrash();

        [DllImport("__Internal")]
        private static extern void CauseBufferOverflowCrash();

        [DllImport("__Internal")]
        private static extern void CauseMutexCrash();

        [DllImport("__Internal")]
        private static extern void CauseAbortCrash();

        [DllImport("__Internal")]
        private static extern void CauseNullVirtualCallCrash();

        [DllImport("__Internal")]
        private static extern void NullReferenceNSException();

        [DllImport("__Internal")]
        private static extern void InvalidFileHandleException();


        [DllImport("__Internal")]
        private static extern void ParseJSONOnBackgroundThread(string jsonString, Action<string> callback);

        [DllImport("__Internal")]
        private static extern void ParseMalformedJSON(string jsonString, Action<string> callback);


        public static void TriggerCauseNullVirtualCallCrash() => CauseNullVirtualCallCrash();
        public static void TriggerNullPointerCrash() => CauseNullPointerCrash();
        public static void TriggerArrayBoundsCrash() => CauseArrayBoundsCrash();
        public static void TriggerDivideByZeroCrash() => CauseDivideByZeroCrash();
        public static void TriggerUnalignedMemoryAccessCrash() => CauseUnalignedMemoryAccessCrash();
        public static void TriggerStackOverflowCrash() => CauseStackOverflowCrash();
        public static void TriggerDoubleFreeCrash() => CauseDoubleFreeCrash();
        public static void TriggerUseAfterFreeCrash() => CauseUseAfterFreeCrash();
        public static void TriggerIllegalInstructionCrash() => CauseIllegalInstructionCrash();
        public static void TriggerInvalidMemoryAccessCrash() => CauseInvalidMemoryAccessCrash();
        public static void TriggerUncaughtExceptionCrash() => CauseUncaughtExceptionCrash();
        public static void TriggerInvalidFunctionPointerCrash() => CauseInvalidFunctionPointerCrash();
        public static void TriggerBufferOverflowCrash() => CauseBufferOverflowCrash();
        public static void TriggerMutexCrash() => CauseMutexCrash();
        public static void TriggerAbortCrash() => CauseAbortCrash();
        public static void TriggerNullReferenceNSException() => NullReferenceNSException();
        public static void TriggerInvalidFileHandleException() => InvalidFileHandleException();

        /*public static void TriggerParseMalformedJSON()
        {
             var jsonString = "{ key: value ";       // Malformed JSON
    
             ParseMalformedJSON(jsonString, NativeCallbackMethod );
        }
        public static void TriggerParseWellformedJSON()
        {
            var jsonString = "{ \"key\": \"value\" }"; // Well-formed JSON
           
            ParseJSONOnBackgroundThread(jsonString, NativeCallbackMethod);
        }*/

        // Define the callback delegate
        public delegate void NativeCallback(string message);


        [MonoPInvokeCallback(typeof(NativeCallback))]
        private static void NativeCallbackMethod(string result)
        {
            Debug.Log($"Native Callback: {result}" + Thread.CurrentThread.Name);
        }



        public static void RunCrashInBackground(System.Action nativeCrashMethod)
        {
            Task.Run(
                () =>
                {
                    try
                    {
                        nativeCrashMethod?.Invoke();
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"RunCrashInBackground Caught exception in Task: {ex.Message}");
                    }
                });
        }

        public void TestDoubleFreeCrash()
        {
            RunCrashInBackground(CauseDoubleFreeCrash);
        }

        /*
         * Crash Behavior When Using Task.Run
           Crash Type	Will It Crash the Whole App?	Explanation
           1. Dereference Null Pointer	🚫 No	A null pointer dereference causes a segmentation fault, but on a background thread, it terminates only that thread.
           2. Array Out of Bounds	🚫 No	Accessing invalid array indices leads to thread termination but not a full app crash.
           3. Divide by Zero	🚫 No (Integer)	Integer division by zero raises an exception, which can be caught in the Task. No app termination.
           4. Unaligned Memory Access	🚫 No	Crashes the thread, not the main app, when run on a background thread.
           5. Infinite Recursion	🚫 No	Causes a stack overflow but only on the background thread.
           6. Double Free	✅ Yes	Double free corrupts the heap, leading to undefined behavior that usually terminates the entire app.
           7. Access Freed Memory	✅ Yes	Use-after-free can corrupt memory and lead to crashes affecting the entire app.
           8. Illegal Instruction	✅ Yes	Illegal instructions cause immediate termination of the app, regardless of the thread.
           9. Invalid Memory Access	✅ Yes	Accessing invalid memory locations results in segmentation faults that crash the entire app.
           10. Uncaught Exception	🚫 No (Handled)	If caught in the Task, it won't crash the app. An uncaught exception in the background thread might terminate the thread but not the app.
           11. Invalid Function Pointer	✅ Yes	Calling invalid function pointers causes undefined behavior, often leading to app termination.
           12. Buffer Overflow	✅ Yes	Buffer overflow corrupts adjacent memory, often affecting the entire process and crashing the app.
           13. Mutex Misuse	🚫 No	Unlocking an uninitialized mutex crashes the thread but not the app if run on a background thread.
           14. Null Virtual Call (C++)	✅ Yes	Causes undefined behavior that often terminates the app.
           15. Trigger abort()	✅ Yes	abort() explicitly terminates the entire app, irrespective of the thread.
         */
    }
}