using System;
using System.Collections.Generic;
using System.Threading;

namespace FakeAsyncs
{
    public class FakeAsyncSynchronizationContext : SynchronizationContext
    {
        private readonly Queue<(SendOrPostCallback Callback, object State)> _queue = new Queue<(SendOrPostCallback Callback, object State)>();
        private object _lockObj = new object();

        public override void Post(SendOrPostCallback d, object state)
        {
            _queue.Enqueue((d, state));
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            throw new NotImplementedException();
        }

        public override int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
        {
            return base.Wait(waitHandles, waitAll, millisecondsTimeout);
        }

        public override SynchronizationContext CreateCopy()
        {
            return this;
        }

        private void RunNextCallback()
        {
            var lockTaken = false;
            Monitor.TryEnter(_lockObj, ref lockTaken);

            if (!lockTaken)
                throw new FakeAsyncConcurrencyException("Only one thread is allowed to run inside FakeAsync. " + FakeAsyncConcurrencyException.DefaultTaskSchedulerWarning);

            try
            {
                var (callback, state) = _queue.Dequeue();
                callback(state);
            }
            finally
            {
                Monitor.Exit(_lockObj);
            }
        }

        /// <summary>
        /// Fake for synchronous waits.
        /// </summary>
        public void FakeSyncWait()
        {
            var ewh = new EventWaitHandle(false, EventResetMode.AutoReset);

        }

    }
}
