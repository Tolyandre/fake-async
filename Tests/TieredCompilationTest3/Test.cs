using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Tests;
using Xunit;
using Xunit.Abstractions;

namespace TieredCompilationTest3
{
    /// <summary>
    /// This test fails if TieredCompilation is on.
    /// </summary>
    public class Test
    {
        private readonly FakeAsyncs.FakeAsync _fakeAsync = new FakeAsyncs.FakeAsync();

        private TimeSpan _delayForJITdoesHisWork = TimeSpan.FromSeconds(5);

        private readonly ITestOutputHelper _testOutputHelper;

        public Test(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void PatchRemainsAfterDelay3()
        {
            //warm up
            _fakeAsync.Isolate(async () => { });

            const int tickStep = 1;
            const int ticks = 60;

           _fakeAsync.Isolate(async () =>
            {
                var testing = MethodUnderTest(ticks, tickStep);

                _fakeAsync.Tick(TimeSpan.FromSeconds(ticks * tickStep));
                _fakeAsync.Tick(TimeSpan.FromSeconds(ticks * tickStep));

                await testing;
            });
           

            //await Task.Delay(_delayForJITdoesHisWork);
            
        }

        private async Task MethodUnderTest(int ticks, int tickStep)
        {
            var stopwatch = new Stopwatch();

            for (int i = 0; i < ticks; i++)
            {
                _testOutputHelper.WriteLine("Part I, iteration {0}", i);

                stopwatch.Restart();

                var task = Task.Run(() => { });
                task.AssertIfFakeTaskScheduler();

                //FakeAsync.ReapplyPatch();
                await Task.Delay(TimeSpan.FromSeconds(tickStep));

                stopwatch.Stop();
                Assert.True(stopwatch.ElapsedMilliseconds < 100, $"Dalay is not faked. Time consumed: {stopwatch.Elapsed}");
            }

            await Task.Yield();

            for (int i = 0; i < ticks; i++)
            {
                _testOutputHelper.WriteLine("Part II, iteration {0}", i);

                stopwatch.Restart();

                var task = Task.Run(() => { });
                task.AssertIfFakeTaskScheduler();

                await Task.Delay(TimeSpan.FromSeconds(tickStep));

                stopwatch.Stop();
                Assert.True(stopwatch.ElapsedMilliseconds < 100, $"Dalay is not faked. Time consumed: {stopwatch.Elapsed}");
            }
        }
    }
}
