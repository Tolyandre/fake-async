using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace FakeAsyncs
{
    public class DelayTasksNotCompletedException : Exception
    {
        public DateTime UtcNow { get; private set; }
        public IReadOnlyList <DateTime> DelayUntilTimes { get; private set; }

        public DelayTasksNotCompletedException(DateTime utcNow, IReadOnlyList<DateTime> delayUntilTimes) 
            : base($"Current UTC time is {utcNow}. One or many Delay tasks are still waiting for time: {string.Join(", ", delayUntilTimes)}. Call Tick() to pass time. Make sure that Tick() call is not blocked by awaiting Task.Delay() which this tick should resume.")
        {
            UtcNow = utcNow;
            DelayUntilTimes = delayUntilTimes;
        }

        public DelayTasksNotCompletedException()
        {
        }

        protected DelayTasksNotCompletedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public DelayTasksNotCompletedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
