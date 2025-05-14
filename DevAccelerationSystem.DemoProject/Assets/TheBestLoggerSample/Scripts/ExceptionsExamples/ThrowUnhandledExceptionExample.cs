using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ThrowUnhandledExceptionExample : MonoBehaviour, IPointerClickHandler
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
        throw new InvalidOperationException("This exception is observed using await!");

        await Task.Delay(100);
    }
}
