using FakeAsyncs;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FakeAsyncTests
{
    public class YieldTests
    {
        private readonly FakeAsync _fakeAsync = new FakeAsync();

        [Fact]
        public void YieldShouldNotThrowFakeAsyncConcurrencyException()
        {
            const int tickStep = 1;

            _fakeAsync.Isolate(async () =>
            {
                var testing = MethodUnderTest();

                _fakeAsync.Tick(TimeSpan.FromSeconds(tickStep * 2));

                await testing;
            });

            static async Task MethodUnderTest()
            {
                await Task.Delay(TimeSpan.FromSeconds(tickStep));

                await Task.Yield();

                await Task.Delay(TimeSpan.FromSeconds(tickStep));
            }
        }

        [Fact]
        public void SynchronizationContextMustByNull()
        {
            const int tickStep = 1;

            _fakeAsync.Isolate(async () =>
            {
                var testing = MethodUnderTest();

                _fakeAsync.Tick(TimeSpan.FromSeconds(tickStep * 2));

                await testing;
            });

            static async Task MethodUnderTest()
            {
                Assert.Null(SynchronizationContext.Current);

                await Task.Delay(TimeSpan.FromSeconds(tickStep));

                Assert.Null(SynchronizationContext.Current);

                await Task.Yield();

                Assert.Null(SynchronizationContext.Current);

                await Task.Delay(TimeSpan.FromSeconds(tickStep));

                Assert.Null(SynchronizationContext.Current);
            }
        }
    }
}
