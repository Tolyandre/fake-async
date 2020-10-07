using FakeTimes;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Tests;
using Xunit;
using Xunit.Abstractions;

namespace TieredCompilationTest3
{
    /// <summary>
    /// This test fail on netcoreapp3.1 if TIERED_COMPILATION_PROTECTION not defined
    /// </summary>
    public class Test
    {
        private readonly FakeAsync _fakeAsync = new FakeAsync();

        private TimeSpan _delayForJITdoesHisWork = TimeSpan.FromSeconds(5);

        private readonly ITestOutputHelper _testOutputHelper;

        public Test(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task PatchRemainsAfterDelay3()
        {
            //warm up
            await _fakeAsync.Isolate(async () => { });

            const int tickStep = 1;
            const int ticks = 60;
            var stopwatch = new Stopwatch();

            var testing = _fakeAsync.Isolate(async () =>
            {
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
            });
           
            _fakeAsync.Tick(TimeSpan.FromSeconds(ticks * tickStep));
            _fakeAsync.Tick(TimeSpan.FromSeconds(ticks * tickStep));
            await Task.Delay(_delayForJITdoesHisWork);

            await testing;
        }
    }
}
