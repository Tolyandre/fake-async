using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    public class TimeInterceptionTests
    {

        public TimeInterceptionTests()
        {
            // warmup
            // first test runs longer and may not meet the time
            FakeTime.FakeTime.Isolate(async t => { }).Wait();
        }

        [Fact]

        public async Task DateTimeNow()
        {
            await FakeTime.FakeTime.Isolate(async time =>
            {
                Assert.Equal(new DateTime(2020, 9, 27), DateTime.Now);
            });
        }

        [Fact]
        public async Task TaskDelay()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            await FakeTime.FakeTime.Isolate(async time =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10));
            });

            stopWatch.Stop();

            Assert.True(stopWatch.ElapsedMilliseconds < 50, $"Dalay is not faked. Time consumed: {stopWatch.Elapsed}");
        }

        [Fact]
        public async Task ThreadSleep()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            await FakeTime.FakeTime.Isolate(async time =>
            {
                Thread.Sleep(TimeSpan.FromSeconds(10));
            });

            stopWatch.Stop();
            Assert.True(stopWatch.ElapsedMilliseconds < 50, $"Sleep is not faked. Time consumed: {stopWatch.Elapsed}");
        }
    }
}
