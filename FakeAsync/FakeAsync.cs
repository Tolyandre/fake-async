using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace FakeAsyncs
{
    /// <summary>
    /// Simulates passage of time to test asynchronous long-running code in synchronous way.
    /// </summary>
    public class FakeAsync
    {
        /// <summary>
        /// FakeAsync instance in current isolated execution context.
        /// This value is null outside of <c>Isolate()</c>'s callback.
        /// </summary>
        public static FakeAsync CurrentInstance => _current.Value;

        private static readonly Harmony _harmony;
        private static readonly AsyncLocal<FakeAsync> _current = new AsyncLocal<FakeAsync>();

        static FakeAsync()
        {
            string harmonyId = typeof(FakeAsync).FullName;
            _harmony = new Harmony(harmonyId);

            _harmony.PatchAll(typeof(FakeAsync).Assembly);
        }

        private DateTime _now;
        private bool _isRunning = false;
        internal DeterministicTaskScheduler DeterministicTaskScheduler { get; private set; } = new DeterministicTaskScheduler();

        // This collection is not thread safe, because one-task per time TaskScheduler is used
        private readonly SortedList<DateTime, TaskCompletionSource<bool>> _waitList = new SortedList<DateTime, TaskCompletionSource<bool>>();

        /// <summary>
        /// Current fake time. This value affects <see cref="DateTime.UtcNow" /> and <see cref="DateTime.Now"/>.
        /// </summary>
        public DateTime UtcNow
        {
            get { return _now; }
            set
            {
                if (_isRunning)
                    throw new InvalidOperationException($"Cannot change {nameof(UtcNow)} when method is running. Use {nameof(Tick)}() to pass time.");

                _now = value;
            }
        }

        /// <summary>
        /// Reapplies patches to dotnet runtime.
        /// 
        /// Normally this method is not needed. It may help when multi tier JITting overrides patches.
        /// But it is better off to disable tiered compilation for now.
        /// </summary>
        public static void ReapplyPatch()
        {
            lock (_harmony)
            {
                _harmony.UnpatchAll();
                _harmony.PatchAll(typeof(FakeAsync).Assembly);
            }
        }

        /// <summary>
        /// Runs callback in mocked environment, isolated from default task scheduler and timers.
        /// 
        /// Asynchronous code will be executed sequentially. Delays will resume only after passing time with <c>Tick()</c>.
        /// </summary>
        /// <param name="methodUnderTest">Callback to be run in isolation.</param>
        public void Isolate(Action methodUnderTest) => Isolate(() =>
        {
            methodUnderTest();
            return Task.CompletedTask;
        });

        /// <summary>
        /// Runs callback in mocked environment, isolated from default task scheduler and timers.
        /// 
        /// Asynchronous code will be executed sequentially. Delays will resume only after passing time with <c>Tick()</c>.
        /// </summary>
        /// <param name="methodUnderTest">Callback to be run in isolation.</param>
        public void Isolate(Func<Task> methodUnderTest)
        {
            if (_current.Value != null)
                throw new InvalidOperationException("FakeAsync calls can not be nested");

            _current.Value = this;

            _isRunning = true;

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
                _current.Value = null;
                SynchronizationContext.SetSynchronizationContext(previousSynchronizationContext);
                _isRunning = false;
            }
        }

        /// <summary>
        /// Passes time and resumes awaited delays.
        /// </summary>
        /// <param name="duration">Amount of time to pass.</param>
        public void Tick(TimeSpan duration)
        {
            var endTick = _now + duration;
            DeterministicTaskScheduler.RunTasksUntilIdle();

            while (_waitList.Count > 0 && _now <= endTick)
            {
                var next = _waitList.First();
                if (next.Key > endTick)
                {
                    break;
                }

                _waitList.RemoveAt(0);
                _now = next.Key;

                // SetResult will also run its continuation task
                next.Value.SetResult(false);

                DeterministicTaskScheduler.RunTasksUntilIdle();
            }

            _now = endTick;
        }

        private static bool _preventTieredCompilationDelayed = false;

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

        internal Task CreateFakeDelay(TimeSpan duration)
        {
            if (duration == TimeSpan.Zero)
                return Task.CompletedTask;

            var tcs = new TaskCompletionSource<bool>(null);

            _waitList.Add(UtcNow + duration, tcs);

            return tcs.Task;
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

                next.Value.SetException(new DelayTasksNotCompletedException(UtcNow, new[] { next.Key }));

                DeterministicTaskScheduler.RunTasksUntilIdle();
            }

            if (times.Any())
            {
                return new DelayTasksNotCompletedException(UtcNow, times);
            }

            return null;
        }
    }
}
