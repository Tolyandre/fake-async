using System;
using System.Runtime.Serialization;

namespace FakeAsyncs
{
    /// <summary>
    /// Indicates a problem in FakeAsync itself.
    /// </summary>
    public class FakeAsyncAssertException : Exception
    {
        public const string DefaultTaskSchedulerWarning =
            "Awaiting task created outside of FakeAsync may lead to this issue. " +
            "Outside tasks are scheduled with default ThreadPoolTaskScheduler, therefore are not intercepted by FakeAsync. Also continuation of such tasks untroduces a new thread from thread pool.";

        public FakeAsyncAssertException()
        {
        }

        public FakeAsyncAssertException(string message) : base(message)
        {
        }

        public FakeAsyncAssertException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected FakeAsyncAssertException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
