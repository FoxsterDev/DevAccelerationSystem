namespace DevAccelerationSystem.Core
{
    public interface IEditorLogger
    {
        void Error(object message);
        void Warning(object message);
        void Info(object message);
    }
}