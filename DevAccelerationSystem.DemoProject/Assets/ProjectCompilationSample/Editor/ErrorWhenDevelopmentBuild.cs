public class ErrorWhenDevelopmentBuild
{
    private void Start()
    {
#if DEVELOPMENT_BUILD
        Debug.Log(Application.version);
#endif
    }
}