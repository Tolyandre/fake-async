using FakeAsyncs;
using System;
using System.Threading.Tasks;
using FakeAsyncTests;
using Xunit;

namespace TieredCompilationTest1
{
    /// <summary>
    /// This test fails if TieredCompilation is on.
    /// </summary>
    public class Test
    {
        private readonly FakeAsync _fakeAsync = new FakeAsync();

        private readonly TimeSpan _delayForJITdoesHisWork = TimeSpan.FromSeconds(5);

        [Fact]
        public async Task PatchRemainsAfterDelay1()
        {
            _ = Task.Run(() => { });

            await Task.Delay(_delayForJITdoesHisWork);

            _fakeAsync.Isolate(() =>
            {
                var t = Task.Run(() => { });
                t.AssertIfFakeTaskScheduler();

                return Task.CompletedTask;
            });
        }
    }
}
