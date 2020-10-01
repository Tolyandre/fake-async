using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FakeTimes
{
    public class FakeTime
    {
        internal static FakeTime CurrentTime => _currentTime.Value
            ?? throw new InvalidOperationException("FakeTime is not initialized");

        private static AsyncLocal<FakeTime> _currentTime = new AsyncLocal<FakeTime>();

        private DateTime _initialDateTime;
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

        public async Task Isolate(Func<Task> methodUnderTest, CancellationToken cancellationToken = default)
        {
            if (_currentTime.Value != null)
                throw new InvalidOperationException("Cannot run isolated test inside another isolated test");

            _currentTime.Value = this;
            Now = InitialDateTime;

            const string harmonyId = "com.github.Tolyandre.fake-time";
            var harmony = new Harmony(harmonyId);

            harmony.PatchAll(typeof(FakeTime).Assembly);

            _started = true;

            // TODO: track tasks to ensure they are completed after method exits
            //Task.Factory.StartNew(() =>
            //{

            //}, CancellationToken.None, TaskCreationOptions.None, new DeterministicTaskScheduler());

            try
            {
                Tick(TimeSpan.Zero);
                await methodUnderTest();
            }
            finally
            {
                harmony.UnpatchAll(harmonyId);
                _currentTime.Value = null;
            }
        }

        private SortedList<DateTime, TaskCompletionSource<bool>> _waitList = new SortedList<DateTime, TaskCompletionSource<bool>>();

        public Task FakeDelay(TimeSpan duration)
        {
            if (duration == TimeSpan.Zero)
                return Task.CompletedTask;

            var tcs = new TaskCompletionSource<bool>();
            _waitList.Add(Now + duration, tcs);

            return tcs.Task;
        }

        public void Tick(TimeSpan duration)
        {
            Now += duration;

            while (_waitList.Count > 0)
            {
                var next = _waitList.First();
                if (next.Key > Now)
                    break;

                next.Value.SetResult(false);
                _waitList.RemoveAt(0);
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
                throw new DalayTasksNotCompletedException("One or many Dalay tasks are still waiting for time: " + times);
            }
        }
    }
}
