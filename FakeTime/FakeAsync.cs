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
        internal static FakeAsync CurrentInstance => _currentInstance.Value;

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
        /// It needs patching before every call, because of multi tier JITting overrides patches
        /// </summary>
        public static void ReapplyPatch()
        {

#if TIERED_COMPILATION_PROTECTION

            lock (_harmony)
            {
                _harmony.UnpatchAll();
                _harmony.PatchAll(typeof(FakeAsync).Assembly);
            }

#endif

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

            await WarmUpToPreventTieredCompilation();

            _currentInstance.Value = this;
            Now = InitialDateTime;

            _started = true;

            var taskFactory = new TaskFactory(CancellationToken.None,
                    TaskCreationOptions.DenyChildAttach, TaskContinuationOptions.None, DeterministicTaskScheduler);

            ReapplyPatch();

            try
            {
                var wrapper = taskFactory.StartNew(async() =>
                {
                    await methodUnderTest();

                    DeterministicTaskScheduler.RunTasksUntilIdle();
                });

                DeterministicTaskScheduler.RunTasksUntilIdle();

                await wrapper.Unwrap();
            }
            finally
            {
                _currentInstance.Value = null;
            }
        }

        private static async Task WarmUpToPreventTieredCompilation()
        {
#if TIERED_COMPILATION_PROTECTION

            _ = Task.Run(() => { });
            _ = Task.Delay(TimeSpan.FromSeconds(0));

            if (!_preventTieredCompilationDelayed)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                _preventTieredCompilationDelayed = true;
            }

#endif
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

                Now = next.Key;

                next.Value.SetResult(false);
                _waitList.RemoveAt(0);

                DeterministicTaskScheduler.RunTasksUntilIdle();
            }
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
