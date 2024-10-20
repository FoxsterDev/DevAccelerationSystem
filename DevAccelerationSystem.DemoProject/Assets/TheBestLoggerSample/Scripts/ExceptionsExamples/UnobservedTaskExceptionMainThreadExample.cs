using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UnobservedTaskExceptionMainThreadExample : MonoBehaviour, IPointerClickHandler
{
    void Start()
    {
        var str = GetType().Name;
        gameObject.name = str + "Button";
        GetComponentInChildren<Text>().text = str.Substring(0, str.Length - "Example".Length);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        RunTaskOnMainThread();

        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    private void RunTaskOnMainThread()
    {
        _ = Task.Factory.StartNew(
            () =>
            {
                throw new InvalidOperationException("This exception will be unobserved, running on the main thread! "+ Thread.CurrentThread.ManagedThreadId);
            }, CancellationToken.None,
            TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
    }
}
