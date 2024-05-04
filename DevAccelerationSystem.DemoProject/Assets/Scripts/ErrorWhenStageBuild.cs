using UnityEngine;

public class ErrorWhenStageBuild : MonoBehaviour
{
#if DEVELOPMENT_BUILD
    public int valuenotused;
#endif 
    #if STAGE_BUILD
public float2 value;
    #endif
    void Start()
    {
        try
        {
            print("!!build");
        }
        finally
        {
            //not used
        }
    }

}