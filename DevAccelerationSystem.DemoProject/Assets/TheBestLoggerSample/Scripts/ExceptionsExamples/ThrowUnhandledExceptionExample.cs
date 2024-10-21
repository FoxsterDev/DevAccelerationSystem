using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ThrowUnhandledExceptionExample : MonoBehaviour, IPointerClickHandler
{
    void Start()
    {
        var str = GetType().Name;
        gameObject.name = str + "Button";
        GetComponentInChildren<Text>().text = str.Substring(0, str.Length - "Example".Length);
    }

    async void RunTaskWithAwait()
    { 
        throw new InvalidOperationException("This exception is observed using await!");
       
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        RunTaskWithAwait();
    }
}