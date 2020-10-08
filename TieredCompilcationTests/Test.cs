using FakeAsyncs;
using System;
using System.Threading.Tasks;
using Tests;
using Xunit;

namespace TieredCompilationTest1
{
    /// <summary>
    /// This test fail on netcoreapp3.1 if TIERED_COMPILATION_PROTECTION not defined
    /// </summary>
    public class Test
    {
        private readonly FakeAsync _fakeAsync = new FakeAsync();

        private TimeSpan _delayForJITdoesHisWork = TimeSpan.FromSeconds(5);

        [Fact]
        public async Task PatchRemainsAfterDelay1()
        {
            _ = Task.Run(() => { });

            await Task.Delay(_delayForJITdoesHisWork);

            await _fakeAsync.Isolate(() =>
            {
                var t = Task.Run(() => { });
                t.AssertIfFakeTaskScheduler();

                return Task.CompletedTask;
            });
        }
    }
}
