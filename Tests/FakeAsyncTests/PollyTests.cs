using FakeAsyncs;
using Polly;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FakeAsyncTests
{
    public class PollyTests
    {
        private readonly FakeAsync _fakeAsync = new FakeAsync();

        [Fact]
        public void WaitAndRetry()
        {
            var policy = Policy
              .Handle<ApplicationException>()
              .WaitAndRetryAsync(new[]
              {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(3)
              });

            int count = 0;
            async Task Method()
            {
                count++;
                throw new ApplicationException();
            }

            _fakeAsync.Isolate(() =>
            {
                var testing = policy.ExecuteAsync(() => Method());

                _fakeAsync.Tick(TimeSpan.FromSeconds(100));
                Assert.Equal(3, count);

                return testing;
            });
        }
    }
}
