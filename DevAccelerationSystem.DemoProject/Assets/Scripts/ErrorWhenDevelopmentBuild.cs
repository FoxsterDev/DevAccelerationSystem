using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ErrorWhenDevelopmentBuild : MonoBehaviour
{
#if DEVELOPMENT_BUILD
    async void Start()
    {
        try
        {
            Debug.Log2("!!!!");
        }
        finally
        {
            //not used
        }
    }
#endif

}