using FakeAsyncs;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Tests
{
    public class TaskSchedulerTests
    {
        private readonly FakeAsyncs.FakeAsync _fakeAsync = new FakeAsyncs.FakeAsync();
        private readonly ITestOutputHelper _testOutputHelper;

        public TaskSchedulerTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task ChangesTaskSchedulerInsideFakeAsync()
        {
            var task = Task.Run(() => { });
            task.AssertIfTheadPoolTaskScheduler();

            _fakeAsync.Isolate(() =>
            {
                var task2 = Task.Run(() => { });
                task2.AssertIfFakeTaskScheduler();

                return Task.CompletedTask;
            });

            Task.Run(() => { }).AssertIfTheadPoolTaskScheduler();
        }
    }
}
