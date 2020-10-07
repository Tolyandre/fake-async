using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FakeTimes
{
    public class FakeAsync
    {
        public static FakeAsync CurrentInstance => _currentInstance.Value;

        internal static AsyncLocal<FakeAsync> _currentInstance = new AsyncLocal<FakeAsync>();

        private DateTime _initialDateTime;

        private static bool _preventTieredCompilationDelayed = false;
        private static Harmony _harmony;

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

        public async Task Isolate(Func<Task> methodUnderTest, CancellationToken cancellationToken = default)
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
                    _ = methodUnderTest();

                    DeterministicTaskScheduler.RunTasksUntilIdle();
                });

                DeterministicTaskScheduler.RunTasksUntilIdle();

                await wrapper;
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
                    Now = endTick;
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
        public void ThrowIfDalayTasksNotCompleted()
        {
            var times = string.Join(", ", _waitList.Select(x => x.Key));
            if (!string.IsNullOrEmpty(times))
            {
                throw new DalayTasksNotCompletedException($"Current time is {Now}. One or many Dalay tasks are still waiting for time: {times}");
            }
        }
    }
}
