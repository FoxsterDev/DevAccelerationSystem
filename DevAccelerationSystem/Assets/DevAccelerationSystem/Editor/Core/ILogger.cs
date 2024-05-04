namespace DevAccelerationSystem.Core
{
    public interface ILogger
    {
        void Error(object message);
        void Warning(object message);
        void Info(object message);
    }
}