using FakeAsyncs;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Tests
{
    public class TaskSchedulerTests
    {
        private readonly FakeAsync _fakeAsync = new FakeAsync();
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

            await _fakeAsync.Isolate(() =>
            {
                var task2 = Task.Run(() => { });
                task2.AssertIfFakeTaskScheduler();

                return Task.CompletedTask;
            });

            Task.Run(() => { }).AssertIfTheadPoolTaskScheduler();
        }
    }
}
