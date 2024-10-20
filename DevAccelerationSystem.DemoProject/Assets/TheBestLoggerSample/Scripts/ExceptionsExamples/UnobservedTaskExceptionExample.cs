using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//CoreLogger.cs:187-[LogFormat][BackThread]-->[Exception] [InvalidOperationException]: This exception will be unobserved! 
public class UnobservedTaskExceptionExample : MonoBehaviour, IPointerClickHandler
{
    void Start()
    {
        var str = GetType().Name;
        gameObject.name = str + "Button";
        GetComponentInChildren<Text>().text = str.Substring(0, str.Length - "Example".Length);
    }

    private void CreateFaultyTask()
    {
        _ = Task.Run(() =>
        {
            throw new InvalidOperationException("This exception will be unobserved!");
        });
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        CreateFaultyTask();
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}
