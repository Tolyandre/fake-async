using System;
using System.Runtime.Serialization;

namespace FakeAsyncs
{
    /// <summary>
    /// Indicates a problem in FakeAsync itself.
    /// </summary>
    public class FakeAsyncConcurrencyException : Exception
    {
        public const string DefaultTaskSchedulerWarning =
            "In general, this issue can occure if TaskScheduler or SynchronizationContext is not faked. " +
            "Awaiting task created outside of FakeAsync may lead to this issue. " +
            "Outside tasks are scheduled with default ThreadPoolTaskScheduler, therefore are not intercepted by FakeAsync. Continuation of such tasks untroduces a new thread from thread pool.";

        public FakeAsyncConcurrencyException()
        {
        }

        public FakeAsyncConcurrencyException(string message) : base(message)
        {
        }

        public FakeAsyncConcurrencyException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected FakeAsyncConcurrencyException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
