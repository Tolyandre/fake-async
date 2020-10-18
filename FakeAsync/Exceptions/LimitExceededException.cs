using System;
using System.Runtime.Serialization;

namespace FakeAsyncs
{
    public class LimitExceededException : Exception
    {
        public static void ThrowIterationLimit(uint iterations)
        {
            throw new LimitExceededException(
                iterations,
                $"Pending tasks still exist after {iterations} iterations. This may indicate infitine asynchronous loop.",
                null);
        }

        public static void ThrowPendingTasksLimit(uint pendingTasksCount)
        {
            throw new LimitExceededException(
                pendingTasksCount,
                $"Limit of pending tasks is exceeded. There are {pendingTasksCount} tasks in task scheduler.",
                null);
        }

        /// <summary>
        /// Current value that exceeded limit.
        /// </summary>
        public uint ExceededValue { get; private set; }

        public LimitExceededException()
        {
        }

        public LimitExceededException(uint exceededValue, string message, Exception innerException) : base(message, innerException)
        {
            ExceededValue = exceededValue;
        }

        protected LimitExceededException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
