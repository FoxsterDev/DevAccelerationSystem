using UnityEngine;

namespace TheBestLogger
{
    internal class TimerManager : MonoBehaviour
    {
        private static TimerManager instance;

        public static TimerManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var timerManagerObject = new GameObject(nameof(TimerManager), typeof(TimerManager));
                    instance = timerManagerObject.GetComponent<TimerManager>();
                    DontDestroyOnLoad(timerManagerObject);
                }

                return instance;
            }
        }
        /*
         * 
    private List<TimerData> timers = new List<TimerData>();

    private struct TimerData
    {
        public float Interval;
        public float ElapsedTime;
        public System.Action Callback;
    }

    void Update()
    {
        float deltaTime = Time.deltaTime;
        for (int i = 0; i < timers.Count; i++)
        {
            TimerData timer = timers[i];
            timer.ElapsedTime += deltaTime;
            if (timer.ElapsedTime >= timer.Interval)
            {
                timer.Callback?.Invoke();
                timer.ElapsedTime = 0f;
            }
            timers[i] = timer;
        }
    }

    public void AddTimer(float interval, System.Action callback)
    {
        timers.Add(new TimerData
        {
            Interval = interval,
            ElapsedTime = 0f,
            Callback = callback
        });
    }

    public void RemoveTimer(System.Action callback)
    {
        timers.RemoveAll(t => t.Callback == callback);
    }
         */
    }
    
    internal static class UnityLogExtension
    {
        public static LogLevel ConvertFromUnityLogType(this LogType unityLogType)
        {
            return unityLogType switch
            {
                LogType.Exception => LogLevel.Exception,
                LogType.Error => LogLevel.Error,
                LogType.Assert => LogLevel.Error,
                LogType.Warning => LogLevel.Warning,
                LogType.Log => LogLevel.Debug,
                _ => LogLevel.Info
            };
        }
    }
}