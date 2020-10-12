using FakeAsyncs;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FakeAsyncTests
{
    public class SimpleTickTests
    {
        private readonly FakeAsyncs.FakeAsync _fakeAsync;

        public SimpleTickTests()
        {
            _fakeAsync = new FakeAsyncs.FakeAsync();
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

        private Task AsyncMethodWithSingleDelay()
        {
            return Task.Delay(TimeSpan.FromSeconds(10));
        }
    }
}
