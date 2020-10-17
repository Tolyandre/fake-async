using FakeAsyncs;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FakeAsyncTests
{
    public class TaskWhenAllTests
    {
        private readonly FakeAsync _fakeAsync = new FakeAsync();

        [Fact]
        public void WaitsAllTasks()
        {
            bool flag1 = false;
            bool flag2 = false;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            async Task ThrowingMethodAsync()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            {
                flag2 = true;
                throw new ApplicationException();
            }

            _fakeAsync.Isolate(() =>
            {
                var task1 = Task.Run(() =>
                {
                    flag1 = true;
                });

                var task2 = ThrowingMethodAsync();

                var testing = Task.WhenAll(task1, task2);

                _fakeAsync.Tick(TimeSpan.Zero);

                Assert.True(testing.IsCompleted);
                Assert.True(testing.IsFaulted);
                Assert.True(flag1);
                Assert.True(flag2);

                return Task.CompletedTask;
            });
        }

        [Fact]
        public void PendingDelayShouldThrow1()
        {
            var startTime = new DateTime(2020, 10, 20, 0, 0, 0, DateTimeKind.Utc);
            _fakeAsync.UtcNow = startTime;

            var ex = Assert.Throws<DelayTasksNotCompletedException>(() => _fakeAsync.Isolate(async () =>
            {
                await Task.WhenAll(
                    Task.Delay(TimeSpan.FromSeconds(10)),
                    Task.Delay(TimeSpan.FromSeconds(60)));
            }));

            Assert.Collection(ex.DelayUntilTimes,
                x => Assert.Equal(startTime + TimeSpan.FromSeconds(10), x));
        }

        [Fact]
        public void PendingDelayShouldThrow2()
        {
            var startTime = new DateTime(2020, 10, 20, 0, 0, 0, DateTimeKind.Utc);
            _fakeAsync.UtcNow = startTime;

            var ex = Assert.Throws<DelayTasksNotCompletedException>(() => _fakeAsync.Isolate(() =>
            {
                var testing = Task.WhenAll(
                    Task.Delay(TimeSpan.FromSeconds(10)),
                    Task.Delay(TimeSpan.FromSeconds(60)));

                _fakeAsync.Tick(TimeSpan.FromSeconds(10));

                return testing;
            }));

            Assert.Collection(ex.DelayUntilTimes,
                x => Assert.Equal(startTime + TimeSpan.FromSeconds(60), x));
        }
    }
}
