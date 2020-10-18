using FakeAsyncs;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FakeAsyncTests
{
    public class TickTests
    {
        private readonly FakeAsync _fakeAsync;

        public TickTests()
        {
            _fakeAsync = new FakeAsync();
            _fakeAsync.UtcNow = new DateTime(2020, 9, 30);
        }

        [Fact]

        public void TickBypassesTime()
        {
            _fakeAsync.Isolate(async () =>
            {
                var task = AsyncMethodWithSingleDelay();

                _fakeAsync.Tick(TimeSpan.FromSeconds(10));

                await task;
            });
        }

        [Fact]
        public void ThrowsIfTimeRemainsAfterTick()
        {
            Assert.Throws<DelayTasksNotCompletedException>(() => _fakeAsync.Isolate(async () =>
            {
                var task = AsyncMethodWithSingleDelay();

                _fakeAsync.Tick(TimeSpan.FromSeconds(9));
            }));
        }

        [Fact]
        public void TickCanPassBigPeriodOfTime()
        {
            _fakeAsync.Isolate(() =>
            {
                var testing = AsyncMethodWithSingleDelay();

                _fakeAsync.Tick(TimeSpan.FromDays(365.4 * 100));
                return testing;
            });

            Assert.Equal(new DateTime(2020, 9, 30).AddDays(365.4 * 100).ToUniversalTime(), _fakeAsync.UtcNow);
        }


        private Task AsyncMethodWithSingleDelay()
        {
            return Task.Delay(TimeSpan.FromSeconds(10));
        }
    }
}
