using FakeAsyncs;
using System;
using System.Threading.Tasks;
using Tests;
using Xunit;

namespace TieredCompilationTest2
{
    /// <summary>
    /// This test fail on netcoreapp3.1 if TIERED_COMPILATION_PROTECTION not defined
    /// </summary>
    public class Test
    {
        private readonly FakeAsync _fakeAsync = new FakeAsync();

        private readonly TimeSpan _delayForJITdoesHisWork = TimeSpan.FromSeconds(5);

        [Fact]
        public async Task PatchRemainsAfterDelay2()
        {
            var realDelay = Task.Delay(_delayForJITdoesHisWork);
            await _fakeAsync.Isolate(async () =>
            {
                var t1 = Task.Run(() => { });
                t1.AssertIfFakeTaskScheduler();

                await realDelay;

                Assert.NotNull(FakeAsync.CurrentInstance);

                //FakeAsync.ReapplyPatch();

                var t2 = Task.Run(() => { });
                t2.AssertIfFakeTaskScheduler();
            });
        }
    }
}
