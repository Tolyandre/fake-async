using FakeAsyncs;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace MixedTaskSchedulersAndDelay
{
    public class Test
    {
        private readonly FakeAsync _fakeAsync = new FakeAsync();

        [Fact]
        public async Task MixedTaskSchedulersAndDelay()
        {
            var stopWatch = new Stopwatch();

            var testing = _fakeAsync.Isolate(async () =>
            {
                stopWatch.Start();

                var task = Task.Run(() => { });

                // fake delay
                await Task.Delay(TimeSpan.FromSeconds(5));

                stopWatch.Stop();
                Assert.True(stopWatch.ElapsedMilliseconds < 500, $"Dalay is not faked. Time consumed: {stopWatch.Elapsed}");

            });

            _fakeAsync.Tick(TimeSpan.FromSeconds(60));

            // real delay (ThreadPoolTaskScheduler)
            await Task.Delay(TimeSpan.FromSeconds(2));

            await testing;
        }
    }
}
