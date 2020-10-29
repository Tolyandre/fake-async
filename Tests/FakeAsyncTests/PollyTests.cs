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
        public void WaitAndRetryAsync()
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

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            async Task Method()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            {
                count++;
                throw new ApplicationException(count.ToString());
            }

            var ex = Assert.Throws<ApplicationException>(() => _fakeAsync.Isolate(() =>
            {
                var testing = policy.ExecuteAsync(() => Method());

                _fakeAsync.Tick(TimeSpan.FromSeconds(100));
                Assert.Equal(4, count);

                return testing;
            }));

            Assert.Equal(4.ToString(), ex.Message);
        }

        [Fact]
        public void WaitAndRetry()
        {
            var policy = Policy
              .Handle<ApplicationException>()
              .WaitAndRetry(new[]
              {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(3)
              });

            int count = 0;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            async Task Method()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            {
                count++;
                throw new ApplicationException(count.ToString());
            }

            var ex = Assert.Throws<ApplicationException>(() => _fakeAsync.Isolate(() =>
            {
                var testing = policy.Execute(() => Method());

                _fakeAsync.Tick(TimeSpan.FromSeconds(100));
                Assert.Equal(4, count);

                return testing;
            }));

            Assert.Equal(4.ToString(), ex.Message);
        }
    }
}
