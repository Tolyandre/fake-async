using System;

namespace FakeAsyncs
{
    public class DalayTasksNotCompletedException : Exception
    {
        public DalayTasksNotCompletedException(string message) : base(message)
        {
        }

        public DalayTasksNotCompletedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
