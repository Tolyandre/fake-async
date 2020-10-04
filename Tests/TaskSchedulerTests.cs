using FakeTimes;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    public class TaskSchedulerTests
    {
        private readonly FakeAsync _fakeAsync = new FakeAsync();

        /// <summary>
        /// Test assumes that delay is enough to flush any tasks on thread pool.
        /// This is not accurate and may depend on side effects.
        /// This is an approximate way to show no concurrency inside FakeAsync.
        /// 
        /// More precise test is <seealso cref="ChangesTaskSchedulerInsideFakeAsync"/>.
        /// </summary>
        [Fact]
        public async Task NoConcurrencyInsideFakeAsync()
        {
            var flag1 = false;
            var flag2 = false;

            // Meta test: delay for a long time flushes background task
            _ = Task.Run(() => flag1 = true);
            await Task.Delay(TimeSpan.FromSeconds(2));
            Assert.True(flag1);

            // Actual test:
            var realDelay = Task.Delay(TimeSpan.FromSeconds(2));
            await _fakeAsync.Isolate(async () =>
            {
                _ = Task.Run(() => flag2 = true);

                // Delay task is created outside of FakeAsync, so it is a real delay with timer.
                await realDelay;

                Assert.False(flag2);
            });

            // When FakeAsync returns, all its task must be completed
            Assert.True(flag2);
        }

        [Fact]
        public async Task ChangesTaskSchedulerInsideFakeAsync()
        {
            var t =TaskScheduler.Default;

            Task.Run(() => { }).AssertIfTheadPoolTaskScheduler();

            await _fakeAsync.Isolate(() =>
            {
                Task.Run(() => { }).AssertIfFakeTaskScheduler();

                return Task.CompletedTask;
            });

            Task.Run(() => { }).AssertIfTheadPoolTaskScheduler();
        }
    }
}
