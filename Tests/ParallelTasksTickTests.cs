using FakeTimes;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    public class ParallelTasksTickTests
    {
        private readonly FakeTime _fakeTime;

        private bool _flag1Done = false;
        private bool _flag2Done = false;
        private bool _flag3Done = false;
        private bool _flag4Done = false;

        public ParallelTasksTickTests()
        {
            _fakeTime = new FakeTime();
            _fakeTime.InitialDateTime = new DateTime(2020, 10, 1, 21, 30, 0);
        }

        [Fact]

        public async Task TickBypassesTime()
        {
            await _fakeTime.Isolate(async () =>
            {
                var task = AsyncMethodWithParallelDelays();

                _fakeTime.Tick(TimeSpan.FromSeconds(31));

                _fakeTime.ThrowIfDalayTasksNotCompleted();

                Assert.True(_flag1Done);
                Assert.True(_flag2Done);
                Assert.True(_flag3Done);
                Assert.True(_flag4Done);

                await task;
            });
        }

        [Fact]

        public async Task ThrowsIfTimeRemainsAfterTick()
        {
            await _fakeTime.Isolate(async () =>
            {
                var task = AsyncMethodWithParallelDelays();

                _fakeTime.Tick(TimeSpan.FromSeconds(9));

                Assert.Throws<DalayTasksNotCompletedException>(() => _fakeTime.ThrowIfDalayTasksNotCompleted());

                Assert.False(_flag1Done);
                Assert.False(_flag2Done);
                Assert.False(_flag3Done);
                Assert.False(_flag4Done);
            });
        }

        [Fact]
        public async Task SerialTicksBypassesTime()
        {
            await _fakeTime.Isolate(async () =>
            {
                var task = AsyncMethodWithParallelDelays();

                _fakeTime.Tick(TimeSpan.FromSeconds(9));

                Assert.False(_flag1Done);
                Assert.False(_flag2Done);
                Assert.False(_flag3Done);
                Assert.False(_flag4Done);

                _fakeTime.Tick(TimeSpan.FromSeconds(1)); // 10s total

                Assert.True(_flag1Done);
                Assert.False(_flag2Done);
                Assert.False(_flag3Done);
                Assert.False(_flag4Done);

                _fakeTime.Tick(TimeSpan.FromSeconds(5)); // 15s total

                Assert.True(_flag1Done);
                Assert.False(_flag2Done);
                Assert.True(_flag3Done);
                Assert.False(_flag4Done);

                _fakeTime.Tick(TimeSpan.FromSeconds(5)); // 20s total

                Assert.True(_flag1Done);
                Assert.True(_flag2Done);
                Assert.True(_flag3Done);
                Assert.False(_flag4Done);

                _fakeTime.Tick(TimeSpan.FromSeconds(11)); // 31s total

                Assert.True(_flag1Done);
                Assert.True(_flag2Done);
                Assert.True(_flag3Done);
                Assert.True(_flag4Done);

                _fakeTime.ThrowIfDalayTasksNotCompleted();

                await task;
            });
        }

        private Task AsyncMethodWithParallelDelays()
        {
            return Task.WhenAll(Flag1And2(), Flag3(), Flag4());
        }

        private async Task Flag1And2()
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
            _flag1Done = true;

            await Task.Delay(TimeSpan.FromSeconds(20));
            _flag2Done = true;
        }

        private async Task Flag3()
        {
            await Task.Delay(TimeSpan.FromSeconds(15));
            _flag3Done = true;
        }

        private async Task Flag4()
        {
            await Task.Delay(TimeSpan.FromSeconds(31));
            _flag4Done = true;
        }
    }
}
