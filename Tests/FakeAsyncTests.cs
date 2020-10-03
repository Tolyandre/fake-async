using FakeTimes;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    public class FakeAsyncTests
    {
        private readonly FakeAsync _fakeAsync = new FakeAsync();

        [Fact]
        public async Task RunsSynchronousCode()
        {
            var flag = false;

            await _fakeAsync.Isolate(() =>
            {
                flag = true;
                return Task.CompletedTask;
            });

            Assert.True(flag);
        }

        [Fact]
        public async Task RunsAsynchronousCode()
        {
            var flag1 = false;
            var flag2 = false;

            await _fakeAsync.Isolate(() =>
            {
                Task.Run(() =>
                {
                    flag1 = true;
                });

                Task.Factory.StartNew(() =>
                {
                    flag2 = true;
                });

                Assert.False(flag1);
                Assert.False(flag2);

                _fakeAsync.Tick(TimeSpan.Zero);

                Assert.True(flag1);
                Assert.True(flag2);

                return Task.CompletedTask;
            });
        }

        [Fact]
        public async Task RunsPendingTaskBeforeReturning()
        {
            var flag = false;

            await _fakeAsync.Isolate(() =>
            {
                var task = Task.Run(() =>
                {
                    flag = true;
                });

                return Task.CompletedTask;
            });

            Assert.True(flag);
        }

        [Fact]
        public async Task RunsChainedTaks()
        {

        }

        [Fact]
        public void DefaultDateIsNow()
        {
            Assert.True(DateTime.Now - _fakeAsync.Now < TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task ThrowsIfNested()
        {
            await _fakeAsync.Isolate(async () =>
            {
                var fakeAsync2 = new FakeAsync();

                var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                {
                    await fakeAsync2.Isolate(() => Task.CompletedTask);
                });

                Assert.Equal("FakeAsync calls can not be nested", exception.Message);
            });
        }
    }
}
