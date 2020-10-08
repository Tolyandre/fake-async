using FakeAsyncs;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    public class ParallelTasksTickTests
    {
        private readonly FakeAsync _fakeAsync;

        private bool _flag1Done = false;
        private bool _flag2Done = false;
        private bool _flag3Done = false;
        private bool _flag4Done = false;

        public ParallelTasksTickTests()
        {
            _fakeAsync = new FakeAsync();
            _fakeAsync.InitialDateTime = new DateTime(2020, 10, 1, 21, 30, 0);
        }

        [Fact]

        public async Task TickBypassesTime()
        {
            await _fakeAsync.Isolate(async () =>
            {
                var task = AsyncMethodWithParallelDelays();

                _fakeAsync.Tick(TimeSpan.FromSeconds(31));

                _fakeAsync.ThrowIfDalayTasksNotCompleted();

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
            await _fakeAsync.Isolate(async () =>
            {
                var task = AsyncMethodWithParallelDelays();

                _fakeAsync.Tick(TimeSpan.FromSeconds(9));

                Assert.Throws<DalayTasksNotCompletedException>(() => _fakeAsync.ThrowIfDalayTasksNotCompleted());

                Assert.False(_flag1Done);
                Assert.False(_flag2Done);
                Assert.False(_flag3Done);
                Assert.False(_flag4Done);
            });
        }

        [Fact]
        public async Task SerialTicksBypassesTime()
        {
            await _fakeAsync.Isolate(async () =>
            {
                var task = AsyncMethodWithParallelDelays();

                _fakeAsync.Tick(TimeSpan.FromSeconds(9));

                Assert.False(_flag1Done);
                Assert.False(_flag2Done);
                Assert.False(_flag3Done);
                Assert.False(_flag4Done);

                _fakeAsync.Tick(TimeSpan.FromSeconds(1)); // 10s total

                Assert.True(_flag1Done);
                Assert.False(_flag2Done);
                Assert.False(_flag3Done);
                Assert.False(_flag4Done);

                _fakeAsync.Tick(TimeSpan.FromSeconds(5)); // 15s total

                Assert.True(_flag1Done);
                Assert.False(_flag2Done);
                Assert.True(_flag3Done);
                Assert.False(_flag4Done);

                _fakeAsync.Tick(TimeSpan.FromSeconds(15)); // 30s total

                Assert.True(_flag1Done);
                Assert.True(_flag2Done);
                Assert.True(_flag3Done);
                Assert.False(_flag4Done);

                _fakeAsync.Tick(TimeSpan.FromSeconds(1)); // 31s total

                Assert.True(_flag1Done);
                Assert.True(_flag2Done);
                Assert.True(_flag3Done);
                Assert.True(_flag4Done);

                _fakeAsync.ThrowIfDalayTasksNotCompleted();

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
