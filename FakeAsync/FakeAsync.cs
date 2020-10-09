using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FakeAsyncs
{
    public class FakeAsync
    {
        public static FakeAsync CurrentInstance => _currentInstance.Value;

        internal static AsyncLocal<FakeAsync> _currentInstance = new AsyncLocal<FakeAsync>();

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

        public Task Isolate(Action methodUnderTest) => Isolate(() =>
        {
            methodUnderTest();
            return Task.CompletedTask;
        });

        public async Task Isolate(Func<Task> methodUnderTest)
        {
            if (_currentInstance.Value != null)
                throw new InvalidOperationException("FakeAsync calls can not be nested");

            _currentInstance.Value = this;
            Now = InitialDateTime;

            _started = true;

            var taskFactory = new TaskFactory(CancellationToken.None,
                    TaskCreationOptions.DenyChildAttach, TaskContinuationOptions.None, DeterministicTaskScheduler);

            try
            {
                var wrapper = taskFactory.StartNew(() =>
                {
                    var task = methodUnderTest();

                    DeterministicTaskScheduler.RunTasksUntilIdle();

                    return task;
                });

                DeterministicTaskScheduler.RunTasksUntilIdle();

                await Task.Yield();

                ThrowDelayTasks();

                ThrowIfDelayTasksNotCompleted();
                await await wrapper;
            }
            finally
            {
                _currentInstance.Value = null;
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
        private SortedList<DateTime, TaskCompletionSource<bool>> _waitList = new SortedList<DateTime, TaskCompletionSource<bool>>();

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

        private void ThrowDelayTasks()
        {
            while (_waitList.Count > 0)
            {
                var next = _waitList.First();
                _waitList.RemoveAt(0);

                next.Value.SetException(new DelayTasksNotCompletedException(Now, new[] { next.Key }));

                DeterministicTaskScheduler.RunTasksUntilIdle();
            }
        }

        /// <summary>
        /// Throws if there are dalay tasks that not expired yet.
        /// </summary>
        private void ThrowIfDelayTasksNotCompleted()
        {
            var times = _waitList.Select(x => x.Key).ToArray();

            if (times.Any())
            {
                throw new DelayTasksNotCompletedException(Now, times);
            }
        }
    }
}
