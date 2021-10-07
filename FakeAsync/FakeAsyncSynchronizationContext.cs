using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace FakeAsyncs
{
    public class FakeAsyncSynchronizationContext : SynchronizationContext
    {
        private readonly Queue<(SendOrPostCallback Callback, object State)> _queue = new Queue<(SendOrPostCallback Callback, object State)>();
        private object _lockObj = new object();
        private FakeAsync _fakeAsync;
        private object _restoreMarker = new object();

        private ManualResetEvent _nextCallbackWaitHandle = new ManualResetEvent(false);

        public FakeAsyncSynchronizationContext(FakeAsync fakeAsync)
        {
            _nextCallbackWaitHandle.SetSkipPatching(true);
            _fakeAsync = fakeAsync;
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            _queue.Enqueue((d, state));
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            var manualResetEvent = new ManualResetEvent(false);
            manualResetEvent.SetSkipPatching(true);

            this.Post(_ =>
            {
                d(state);
                manualResetEvent.Set();
            }, null);

            manualResetEvent.WaitOne();
            manualResetEvent.Dispose();
        }

        public override int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
        {
            var manualResetEvent = new ManualResetEvent(false);
            manualResetEvent.SetSkipPatching(true);

            var timeoutTask = _fakeAsync.CreateDelayTask(TimeSpan.FromMilliseconds(millisecondsTimeout), CancellationToken.None)
                .ContinueWith((_, state) =>
                {
                    ((ManualResetEvent)state).Set();
                }, manualResetEvent);

            waitHandles = waitHandles.Union(new[] { manualResetEvent.SafeWaitHandle.DangerousGetHandle() })
                .ToArray();

            int result = base.Wait(waitHandles, waitAll, Timeout.Infinite);

            if (timeoutTask.IsCompleted)
                result = WaitHandle.WaitTimeout;

            manualResetEvent.Dispose();

            return result;
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

            _nextCallbackWaitHandle.Reset();
            Exception exception = null;

            ThreadPool.QueueUserWorkItem((_) =>
            {
                object state = null;
                SendOrPostCallback callback;
                try
                {
                    (callback, state) = _queue.Dequeue();
                    callback(state);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                finally
                {
                    if (state != _restoreMarker)
                        _nextCallbackWaitHandle.Set();
                }
            });

            _nextCallbackWaitHandle.WaitOne();

            Monitor.Exit(_lockObj);

            if (exception != null)
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
            }
        }

        /// <summary>
        /// Fake for synchronous waits.
        /// </summary>
        internal void FakeWait(Action waitAction)
        {
            var manualResetEvent = new ManualResetEvent(false);
            manualResetEvent.SetSkipPatching(true);

            _nextCallbackWaitHandle.Set();

            waitAction();

            //_queue.Enqueue((d, state));
            _queue.Enqueue((_ =>
            {
                manualResetEvent.Set();
            }, _restoreMarker));

            manualResetEvent.WaitOne();
        }
    }
}
