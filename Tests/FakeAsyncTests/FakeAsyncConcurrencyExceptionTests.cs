using FakeAsyncs;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    public class FakeAsyncConcurrencyExceptionTests
    {
        private readonly FakeAsync _fakeAsync = new FakeAsync();

        [Fact]
        public void AwaitExternalDelayThrows1()
        {
            var realDelay = Task.Delay(TimeSpan.FromSeconds(10));

            var ex = Assert.Throws<FakeAsyncConcurrencyException>(() => _fakeAsync.Isolate(async () =>
            {
                await realDelay;
            }));

            Assert.Contains("Task is still not completed but method under isolation returned.", ex.Message);
        }

        [Fact]
        public void AwaitExternalDelayThrows2()
        {
            var realDelay = Task.Delay(TimeSpan.FromSeconds(10));

            Assert.Throws<FakeAsyncConcurrencyException>(() => _fakeAsync.Isolate(async () =>
            {
                var t1 = Task.Run(() => { });
                t1.AssertIfFakeTaskScheduler();

                await realDelay;

                Assert.NotNull(FakeAsync.CurrentInstance);

                var t2 = Task.Run(() => { });
                t2.AssertIfFakeTaskScheduler();
            }));
        }

        [Fact(Skip = "Cannot reproduce for now")]
        public void AwaitExternalTaskThrows()
        {
            static async Task AsyncMethod()
            {
                await Task.Yield();
            }

            var task = AsyncMethod();

            Assert.Throws<FakeAsyncConcurrencyException>(() => _fakeAsync.Isolate(async () =>
            {
                await task;
            }));
        }
    }
}
