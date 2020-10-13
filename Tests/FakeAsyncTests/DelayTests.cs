using FakeAsyncs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FakeAsyncTests
{
    public class DelayTests
    {
        private readonly FakeAsync _fakeAsync = new FakeAsync();

        private readonly DateTime _startTime = new DateTime(2020, 10, 20);

        public DelayTests()
        {
            _fakeAsync.UtcNow = _startTime;
        }

        [Fact]
        public void PendingDelayShouldThrow()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            var ex = Assert.Throws<DelayTasksNotCompletedException>(() => _fakeAsync.Isolate(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10));
            }));

            Assert.Equal(ex.Now, _startTime);
            Assert.Collection(ex.DelayUntilTimes, x => Assert.Equal(x, _startTime + TimeSpan.FromSeconds(10)));
        }

        [Fact]
        public void PendingDiscardedDelayShouldThrow()
        {
            var ex = Assert.Throws<DelayTasksNotCompletedException>(() => _fakeAsync.Isolate(() =>
            {
                // all overloads
                _ = Task.Delay(750);
                _ = Task.Delay(500, CancellationToken.None);
                _ = Task.Delay(TimeSpan.FromSeconds(20));
                _ = Task.Delay(TimeSpan.FromSeconds(10), CancellationToken.None);

                return Task.CompletedTask;
            }));

            Assert.Equal(ex.Now, _startTime);
            Assert.Collection(ex.DelayUntilTimes,
                x => Assert.Equal(x, _startTime + TimeSpan.FromMilliseconds(500)),
                x => Assert.Equal(x, _startTime + TimeSpan.FromMilliseconds(750)),
                x => Assert.Equal(x, _startTime + TimeSpan.FromSeconds(10)),
                x => Assert.Equal(x, _startTime + TimeSpan.FromSeconds(20)));
        }

        [Fact]
        public void ExceptionHasMessage()
        {
            CultureInfo.CurrentCulture = new CultureInfo("ru-RU");

            var ex = Assert.Throws<DelayTasksNotCompletedException>(() => _fakeAsync.Isolate(()
                 => Task.Delay(TimeSpan.FromSeconds(10))));

            Assert.Contains("Current time is 20.10.2020 0:00:00. One or many Delay tasks are still waiting for time: 20.10.2020 0:00:10", ex.Message);
        }

        [Fact]
        public void CompletedDelaysAreNotShownInException()
        {
            var ex = Assert.Throws<DelayTasksNotCompletedException>(() => _fakeAsync.Isolate(() =>
            {
                var cts = new CancellationTokenSource();

                _ = Task.Delay(TimeSpan.FromSeconds(20), cts.Token);
                _ = Task.Delay(1000, cts.Token);
                _ = Task.Delay(TimeSpan.FromSeconds(0.75));
                _ = Task.Delay(500);

                _fakeAsync.Tick(TimeSpan.FromSeconds(0.75));

                return Task.CompletedTask;
            }));

            Assert.Equal(ex.Now, _startTime.AddSeconds(0.75));
            Assert.Collection(ex.DelayUntilTimes,
                x => Assert.Equal(x, _startTime + TimeSpan.FromSeconds(1)),
                x => Assert.Equal(x, _startTime + TimeSpan.FromSeconds(20)));
        }

        [Fact]
        public void CancelledDelaysAreNotShownInException()
        {
            var ex = Assert.Throws<DelayTasksNotCompletedException>(() => _fakeAsync.Isolate(() =>
            {
                var cts = new CancellationTokenSource();

                _ = Task.Delay(TimeSpan.FromSeconds(20), cts.Token);
                _ = Task.Delay(1000, cts.Token);
                _ = Task.Delay(TimeSpan.FromSeconds(0.75));
                _ = Task.Delay(500);

                cts.Cancel();

                return Task.CompletedTask;
            }));

            Assert.Equal(ex.Now, _startTime);
            Assert.Collection(ex.DelayUntilTimes,
                x => Assert.Equal(x, _startTime + TimeSpan.FromSeconds(0.5)),
                x => Assert.Equal(x, _startTime + TimeSpan.FromSeconds(0.75)));
        }

        [Fact]
        public void DoesNotThrowIfDelaysCompleted()
        {
            _fakeAsync.Isolate(async () =>
            {
                _ = Task.Delay(5000);

                _fakeAsync.Tick(TimeSpan.FromSeconds(5));
            });
        }

        [Fact]
        public void ResumesDelaysInTheirOrder()
        {
            _fakeAsync.Isolate(() =>
            {
                var list = new List<int>();

                Task.Delay(1000)
                    .ContinueWith(_ => list.Add(1000));

                Task.Delay(500)
                    .ContinueWith(_ => list.Add(500));

                Task.Delay(2000)
                    .ContinueWith(_ => list.Add(2000));

                Task.Delay(0)
                    .ContinueWith(_ => list.Add(0));

                _fakeAsync.Tick(TimeSpan.FromMilliseconds(2000));

                Assert.Collection(list,
                    x => Assert.Equal(0, x),
                    x => Assert.Equal(500, x),
                    x => Assert.Equal(1000, x),
                    x => Assert.Equal(2000, x));

                return Task.CompletedTask;
            });
        }

        [Fact]
        public void DoesNotResumeCancelledDelays()
        {
            var list = new List<int>();
            var cts = new CancellationTokenSource();

            async Task Method()
            {
                await Task.Delay(500, cts.Token);
                list.Add(500);

                await Task.Delay(1000, cts.Token);
                list.Add(1000);
            }

            _fakeAsync.Isolate(() =>
            {
                var testing = Method();

                _fakeAsync.Tick(TimeSpan.FromMilliseconds(500));
                cts.Cancel();
                _fakeAsync.Tick(TimeSpan.FromMilliseconds(1000));

                Assert.Collection(list,
                    x => Assert.Equal(500, x));

                return testing;
                return Task.CompletedTask;
            });
        }
    }
}
