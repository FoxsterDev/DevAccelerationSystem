using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ObservedTaskExceptionExample : MonoBehaviour, IPointerClickHandler
{
    private void Start()
    {
        var str = GetType().Name;
        gameObject.name = str + "Button";
        GetComponentInChildren<Text>().text = str.Substring(0, str.Length - "Example".Length);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        RunTaskWithAwait();
    }

    private async void RunTaskWithAwait()
    {
        try
        {
            // Run a task that throws an exception
            await Task.Run(() => { throw new InvalidOperationException("This exception is observed using await!"); });
        }
        catch (Exception ex)
        {
            // The exception is observed here
            Debug.LogException(ex);
        }
    }
}
