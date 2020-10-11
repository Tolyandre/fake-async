using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace FakeAsyncs
{
    public class FakeAsync
    {
        public static FakeAsync CurrentInstance => _currentInstance.Value;

        private static readonly AsyncLocal<FakeAsync> _currentInstance = new AsyncLocal<FakeAsync>();

        private DateTime _initialDateTime;

        private static bool _preventTieredCompilationDelayed = false;
        private static readonly Harmony _harmony;

        static FakeAsync()
        {
            string harmonyId = typeof(FakeAsync).FullName;
            _harmony = new Harmony(harmonyId);

            _harmony.PatchAll(typeof(FakeAsync).Assembly);
        }

        /// <summary>
        /// May help when multi tier JITting overrides patches.
        /// </summary>
        public static void ReapplyPatch()
        {
            lock (_harmony)
            {
                _harmony.UnpatchAll();
                _harmony.PatchAll(typeof(FakeAsync).Assembly);
            }
        }

        public DateTime InitialDateTime
        {
            get { return _initialDateTime; }
            set
            {
                if (_started)
                    throw new InvalidOperationException($"Cannot change {nameof(InitialDateTime)} after test started");

                _initialDateTime = value;
            }
        }

        public DateTime Now { get; private set; } = DateTime.Now;

        private bool _started = false;

        internal DeterministicTaskScheduler DeterministicTaskScheduler { get; private set; } = new DeterministicTaskScheduler();

        public void Isolate(Action methodUnderTest) => Isolate(() => methodUnderTest());

        public void Isolate(Func<Task> methodUnderTest)
        {
            if (_currentInstance.Value != null)
                throw new InvalidOperationException("FakeAsync calls can not be nested");

            _currentInstance.Value = this;
            Now = InitialDateTime;

            _started = true;

            var taskFactory = new TaskFactory(CancellationToken.None,
                    TaskCreationOptions.DenyChildAttach, TaskContinuationOptions.None, DeterministicTaskScheduler);

            // YieldAwaiter (Task.Yield()) posts to synchronization context if it exists.
            // https://github.com/dotnet/runtime/blob/61d444ae7ca77cb49f38d313da6defa66f6ca38a/src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/YieldAwaitable.cs#L88
            // This leads to concurrency between our custom task scheduler and tasks running in synchronization context thread.
            // Clearing synchronization context makes YieldAwaiter to use current task scheduler
            var previousSynchronizationContext = SynchronizationContext.Current;
            try
            {
                SynchronizationContext.SetSynchronizationContext(null);

                // TODO: consider Task.RunSynchronously()
                var wrapper = taskFactory.StartNew(() =>
                {
                    var task = methodUnderTest();

                    DeterministicTaskScheduler.RunTasksUntilIdle();

                    return task;
                });

                DeterministicTaskScheduler.RunTasksUntilIdle();

                var delayTasksNotCompletedException = ThrowDelayTasks();

                var unwrappedTask = wrapper.Unwrap();

                if (!unwrappedTask.IsCompleted)
                {
                    throw new FakeAsyncConcurrencyException("Task is still not completed but method under isolation returned. " +
                        "This indicates that some concurrency is not handled by FakeAsync. " + FakeAsyncConcurrencyException.DefaultTaskSchedulerWarning);
                }

                // scenario when delays are not awaited in testing method
                if (delayTasksNotCompletedException != null && !unwrappedTask.IsFaulted)
                {
                    throw delayTasksNotCompletedException;
                }

                // else propagate exceptions from testing method
                if (unwrappedTask.IsFaulted)
                {
                    AggregateException ex = unwrappedTask.Exception;

                    // unwrap AggregatException without changing the stack-trace
                    // to be more like the synchronous code
                    if (ex.InnerExceptions.Count == 1)
                        ExceptionDispatchInfo.Capture(ex.InnerExceptions[0]).Throw();
                    else
                        ExceptionDispatchInfo.Capture(unwrappedTask.Exception).Throw();
                }
            }
            finally
            {
                _currentInstance.Value = null;
                SynchronizationContext.SetSynchronizationContext(previousSynchronizationContext);
            }
        }

        public static async Task WarmUpToEscapeFromTieredCompilation()
        {
            _ = Task.Run(() => { });
            _ = Task.Delay(TimeSpan.FromSeconds(0));

            if (!_preventTieredCompilationDelayed)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                _preventTieredCompilationDelayed = true;
            }
        }

        // This collection is not concurrent, because one-task per time TaskScheduler is used
        private readonly SortedList<DateTime, TaskCompletionSource<bool>> _waitList = new SortedList<DateTime, TaskCompletionSource<bool>>();

        public Task FakeDelay(TimeSpan duration)
        {
            if (duration == TimeSpan.Zero)
                return Task.CompletedTask;

            var tcs = new TaskCompletionSource<bool>(null);

            _waitList.Add(Now + duration, tcs);

            return tcs.Task;
        }

        public void Tick(TimeSpan duration)
        {
            var endTick = Now + duration;
            DeterministicTaskScheduler.RunTasksUntilIdle();

            while (_waitList.Count > 0 && Now <= endTick)
            {
                var next = _waitList.First();
                if (next.Key > endTick)
                {
                    break;
                }

                _waitList.RemoveAt(0);
                Now = next.Key;

                // SetResult will also run its continuation task
                next.Value.SetResult(false);

                DeterministicTaskScheduler.RunTasksUntilIdle();
            }

            Now = endTick;
        }

        /// <summary>
        /// Throws if there are dalay tasks that not expired yet.
        /// </summary>
        private DelayTasksNotCompletedException ThrowDelayTasks()
        {
            var times = _waitList.Select(x => x.Key).ToArray();

            while (_waitList.Count > 0)
            {
                var next = _waitList.First();
                _waitList.RemoveAt(0);

                next.Value.SetException(new DelayTasksNotCompletedException(Now, new[] { next.Key }));

                DeterministicTaskScheduler.RunTasksUntilIdle();
            }

            if (times.Any())
            {
                return new DelayTasksNotCompletedException(Now, times);
            }

            return null;
        }
    }
}
