using FakeTimes;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    public class SimpleTickTests
    {
        private readonly FakeAsync _fakeAsync;

        public SimpleTickTests()
        {
            _fakeAsync = new FakeAsync();
            _fakeAsync.InitialDateTime = new DateTime(2020, 9, 30);
        }

        [Fact]

        public async Task TickBypassesTime()
        {
            await _fakeAsync.Isolate(async () =>
            {
                var task = AsyncMethodWithSingleDelay();

                _fakeAsync.Tick(TimeSpan.FromSeconds(10));

                _fakeAsync.ThrowIfDalayTasksNotCompleted();

                await task;
            });
        }

        [Fact]
        public async Task ThrowsIfTimeRemainsAfterTick()
        {
            await _fakeAsync.Isolate(async () =>
            {
                var task = AsyncMethodWithSingleDelay();

                _fakeAsync.Tick(TimeSpan.FromSeconds(9));

                Assert.Throws<DalayTasksNotCompletedException>(() => _fakeAsync.ThrowIfDalayTasksNotCompleted());
            });
        }

        private Task AsyncMethodWithSingleDelay()
        {
            return Task.Delay(TimeSpan.FromSeconds(10));
        }
    }
}
