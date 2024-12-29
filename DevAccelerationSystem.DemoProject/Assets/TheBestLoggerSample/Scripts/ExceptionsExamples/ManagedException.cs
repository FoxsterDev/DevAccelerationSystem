using System;
using System.Threading.Tasks;

namespace TheBestLogger.Examples
{
    public class ManagedException
    {
        public void ThrowHandledExceptionInVoid()
        {
            try
            {
                throw new ArgumentException("Some error in void method");
            }
            catch (Exception ex)
            {
                GameLogger.Main.LogException(ex, new LogAttributes(LogImportance.Critical));
            }
        }

        public async void ThrowHandledExceptionInAsyncVoid()
        {
            try
            {
                await Task.Delay(10);
                throw new ArgumentException("Some error in async void method");
            }
            catch (Exception ex)
            {
                GameLogger.Main.LogException(ex, new LogAttributes(LogImportance.Critical));
            }
        }
    }
}
