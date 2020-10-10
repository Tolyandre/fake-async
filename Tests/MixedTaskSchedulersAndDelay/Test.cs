using FakeAsyncs;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace MixedTaskSchedulersAndDelay
{
    public class Test
    {
        private readonly FakeAsyncs.FakeAsync _fakeAsync = new FakeAsyncs.FakeAsync();

        [Fact]
        public async Task MixedTaskSchedulersAndDelay()
        {
            var stopWatch = new Stopwatch();

            _fakeAsync.Isolate(async () =>
            {
                stopWatch.Start();

                var task = Task.Run(async () =>
                {
                    // fake delay
                    await Task.Delay(TimeSpan.FromSeconds(5));
                });

                _fakeAsync.Tick(TimeSpan.FromSeconds(60));

                await task;

                stopWatch.Stop();
                Assert.True(stopWatch.ElapsedMilliseconds < 500, $"Dalay is not faked. Time consumed: {stopWatch.Elapsed}");
            });

           

            // real delay (ThreadPoolTaskScheduler)
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
}
