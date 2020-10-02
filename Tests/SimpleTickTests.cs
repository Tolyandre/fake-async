using FakeTimes;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    [CollectionDefinition("SimpleTickTests", DisableParallelization = true)]
    public class SimpleTickTests
    {
        private readonly FakeTime _fakeTime;

        public SimpleTickTests()
        {
            _fakeTime = new FakeTime();
            _fakeTime.InitialDateTime = new DateTime(2020, 9, 30);
        }

        [Fact]

        public async Task TickBypassesTime()
        {
            await _fakeTime.Isolate(async () =>
            {
                var task = AsyncMethodWithSingleDelay();

                _fakeTime.Tick(TimeSpan.FromSeconds(10));

                _fakeTime.ThrowIfDalayTasksNotCompleted();

                await task;
            });
        }

        [Fact]

        public async Task ThrowsIfTimeRemainsAfterTick()
        {
            await _fakeTime.Isolate(async () =>
            {
                var task = AsyncMethodWithSingleDelay();

                _fakeTime.Tick(TimeSpan.FromSeconds(9));

                Assert.Throws<DalayTasksNotCompletedException>(() => _fakeTime.ThrowIfDalayTasksNotCompleted());
            });
        }

        private Task AsyncMethodWithSingleDelay()
        {
            return Task.Delay(TimeSpan.FromSeconds(10));
        }
    }
}
