using System;
using System.Threading.Tasks;

namespace TheBestLogger.Examples
{
    public class AggregateExceptionExample
    {
        public void ThrowHandledException()
        {
            // Create an array of tasks that may result in exceptions
            Task[] tasks = new Task[3]
            {
                Task.Run(() => throw new InvalidOperationException("InvalidOperationException: Some invalid operation!")),
                Task.Run(() => throw new ArgumentNullException("ArgumentNullException: Some argument null error")),
                Task.Run(() => GameLogger.Main.LogInfo("Just log info"))
            };

            try
            {
                // Wait for all tasks to complete
                Task.WaitAll(tasks);
            }
            catch (AggregateException ae)
            {
                // Handle each exception individually
                /*foreach (var ex in ae.InnerExceptions)
                {
                    Console.WriteLine($"Exception type: {ex.GetType()}. Message: {ex.Message}");
                }*/

                GameLogger.Main.LogException(ae, new LogAttributes(LogImportance.Critical));
            }
        }
    }
}
