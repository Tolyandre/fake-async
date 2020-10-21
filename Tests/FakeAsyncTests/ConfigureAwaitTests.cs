using FakeAsyncs;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FakeAsyncTests
{
    public class ConfigureAwaitTests
    {
        private readonly FakeAsync _fakeAsync = new FakeAsync();

        [Fact]
        public void DoesNotUseThreadPool()
        {
            async Task Method()
            {
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);

                Assert.IsType<DeterministicTaskScheduler>(TaskScheduler.Current);

                await Task.Delay(TimeSpan.FromSeconds(10));
            }

            _fakeAsync.Isolate(() =>
            {
                var testing = Method();

                _fakeAsync.Tick(TimeSpan.FromSeconds(11));

                return testing;
            });
        }
    }
}
