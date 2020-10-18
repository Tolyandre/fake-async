using FakeAsyncs;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FakeAsyncTests
{
    public class LimitExceededExceptionTests
    {
        private readonly FakeAsync _fakeAsync = new FakeAsync();

        [Fact]
        public void InfiniteTaskThrows()
        {
            async Task LoopAsync()
            {
                while (true)
                    await Task.Delay(2000);
            }

            var ex = Assert.Throws<LimitExceededException>(() => _fakeAsync.Isolate(() =>
            {
                _ = LoopAsync();

                _fakeAsync.Tick(TimeSpan.FromDays(365));

                return Task.CompletedTask;
            }));

            Assert.Equal((uint)1000, ex.ExceededValue); // default iterations limit
        }

        [Fact]
        public void ForkBombThrows()
        {
            async Task BombAsync()
            {
                await Task.Yield(); // prevent recursive synchronous call

                _ = BombAsync();
                _ = BombAsync();
            }

            var ex = Assert.Throws<LimitExceededException>(() => _fakeAsync.Isolate(() =>
            {
                _ = BombAsync();

                _fakeAsync.Tick(TimeSpan.FromDays(365));

                return Task.CompletedTask;
            }));

            // expected task count with pending tasks limit of 100_000
            uint tasksInBomb = (uint)Math.Pow(2, 17);
            Assert.Equal(tasksInBomb, ex.ExceededValue);
        }
    }
}
