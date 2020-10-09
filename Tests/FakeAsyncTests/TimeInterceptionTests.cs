using FakeAsyncs;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    public class TimeInterceptionTests
    {
        private readonly FakeAsync _fakeAsync;

        public TimeInterceptionTests()
        {
            _fakeAsync = new FakeAsync();
            _fakeAsync.InitialDateTime = new DateTime(2020, 9, 27);
        }

        [Fact]

        public async Task DateTimeNow()
        {
            await _fakeAsync.Isolate(async () =>
            {
                Assert.Equal(new DateTime(2020, 9, 27), DateTime.Now);
                Assert.Equal(new DateTime(2020, 9, 27), DateTime.Now);
            });
        }

        [Fact]
        public async Task TaskDelay()
        {
            var stopWatch = new Stopwatch();

            //warm up
            await _fakeAsync.Isolate(async () => { });

            stopWatch.Start();

            await _fakeAsync.Isolate(async () =>
            {
                var delay = Task.Delay(TimeSpan.FromSeconds(10));

                _fakeAsync.Tick(TimeSpan.FromSeconds(10));

                await delay;
            });

            stopWatch.Stop();

            Assert.True(stopWatch.ElapsedMilliseconds < 500, $"Dalay is not faked. Time consumed: {stopWatch.Elapsed}");
        }

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

        [Fact]
        public async Task SerialTaskDelay()
        {
            var stopWatch = new Stopwatch();

            //warm up
            await _fakeAsync.Isolate(async () => { });

            stopWatch.Start();

            await _fakeAsync.Isolate(async () =>
            {
                var delay1 = AsyncMethod1();
                var delay2 = AsyncMethod2();

                _fakeAsync.Tick(TimeSpan.FromSeconds(11));

                await Task.WhenAll(delay1, delay2);
                //await delay1;
                //await delay2;
            });

            stopWatch.Stop();

            Assert.True(stopWatch.ElapsedMilliseconds < 500, $"Dalay is not faked. Time consumed: {stopWatch.Elapsed}");
        }

        private async Task AsyncMethod1()
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
        }

        private async Task AsyncMethod2()
        {
            await Task.Delay(TimeSpan.FromSeconds(11));
        }

        [Fact]
        public async Task ThreadSleep()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            await _fakeAsync.Isolate(async () =>
            {
                Thread.Sleep(TimeSpan.FromSeconds(10));
            });

            stopWatch.Stop();
            Assert.True(stopWatch.ElapsedMilliseconds < 500, $"Sleep is not faked. Time consumed: {stopWatch.Elapsed}");
        }
    }
}
