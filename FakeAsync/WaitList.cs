using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FakeAsyncs
{
    /// <summary>
    /// Handles delay task and their resume time.
    /// </summary>
    /// <remarks>
    /// This class is not thread safe as our task scheduler runs one task at a time.
    /// </remarks>
    class WaitList
    {
        private readonly LinkedList<(DateTime Time, TaskCompletionSource<bool> Tcs)> _list;

        public WaitList()
        {
            _list = new LinkedList<(DateTime Time, TaskCompletionSource<bool> Tcs)>();
        }

        /// <summary>
        /// Adds a pending delay to the list.
        /// </summary>
        /// <param name="dateTime">Time when delay will be resumed.</param>
        /// <param name="tcs">Task's TaskCompletionSource.</param>
        public void Add(DateTime dateTime, TaskCompletionSource<bool> tcs)
        {
            var node = _list.First;

            while (node != null && node.Value.Time <= dateTime)
                node = node.Next;

            if (node != null)
                _list.AddBefore(node, (dateTime, tcs));
            else
                _list.AddLast((dateTime, tcs));
        }

        /// <summary>
        /// Resumes next pending task within <c>endTick</c>.
        /// If such task exists, <c>now</c> is adjusted to task's completion time.
        /// </summary>
        /// <param name="now">Current time.</param>
        /// <param name="endTick">End of considering period</param>
        /// <returns>True if a task is processed. Otherwise, false.</returns>
        public bool TryResumeNext(ref DateTime now, DateTime endTick)
        {
            if (_list.First == null)
                return false;

            var nextTask = _list.First.Value;

            if (nextTask.Time > endTick)
                return false;

            _list.RemoveFirst();

            now = nextTask.Time;

            // SetResult will also run its continuation task
            nextTask.Tcs.SetResult(false);

            return true;
        }

        /// <summary>
        /// Throws <see cref="DelayTasksNotCompletedException"/> for next task.
        /// </summary>
        /// <param name="utcNow">Current time for exception message.</param>
        /// <param name="time">Returns task's time.</param>
        /// <returns>True if task is processed. Otherwise, false.</returns>
        public bool ThrowNext(DateTime utcNow, out DateTime time)
        {
            if (_list.First == null)
            {
                time = default;
                return false;
            }

            var nextTask = _list.First.Value;
            _list.RemoveFirst();

            nextTask.Tcs.SetException(new DelayTasksNotCompletedException(utcNow, new[] { nextTask.Time }));

            time = nextTask.Time;
            return true;
        }

        public void RemoveAndCancel(TaskCompletionSource<bool> tcs)
        {
            var node = _list.First;

            while (node != null && node.Value.Tcs != tcs)
                node = node.Next;

            // if node is not found, task is already completed
            if (node != null)
            {
                _list.Remove(node);
                tcs.SetCanceled();
            }
        }
    }
}
