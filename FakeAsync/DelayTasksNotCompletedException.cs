using System;

namespace FakeAsyncs
{
    public class DelayTasksNotCompletedException : Exception
    {
        public DateTime Now { get; private set; }
        public DateTime[] DelayUntilTimes { get; private set; }

        public DelayTasksNotCompletedException(DateTime now, DateTime[] delayUntilTimes) 
            : base($"Current time is {now}. One or many Delay tasks are still waiting for time: {string.Join(", ", delayUntilTimes)}")
        {
            Now = now;
            DelayUntilTimes = delayUntilTimes;
        }
    }
}
