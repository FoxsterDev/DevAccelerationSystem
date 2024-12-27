using UnityEngine;

namespace TheBestLogger
{
    [System.Serializable]
    internal class UniTaskConfiguration
    {
        [Tooltip("Propagate OperationCanceledException to UnobservedTaskException when true. Default is false.")]
        public bool PropagateOperationCanceledException = false;

        [Tooltip("Write log type when catch unobserved exception and not registered UnobservedTaskException. Default is Exception.")]
        public  UnityEngine.LogType UnobservedExceptionWriteLogType = UnityEngine.LogType.Exception; 
        [Tooltip("By default it is true in Unitask package but TheBestLogger logtargets have main thread dispatching out of box")]
        public  bool DispatchUnityMainThread = false;
    }
}
