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

        private readonly DateTime _startTime = new DateTime(2020, 10, 20).ToUniversalTime();

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

            Assert.Equal(ex.UtcNow, _startTime);
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

            Assert.Equal(ex.UtcNow, _startTime);
            Assert.Collection(ex.DelayUntilTimes,
                x => Assert.Equal(x, _startTime + TimeSpan.FromMilliseconds(500)),
                x => Assert.Equal(x, _startTime + TimeSpan.FromMilliseconds(750)),
                x => Assert.Equal(x, _startTime + TimeSpan.FromSeconds(10)),
                x => Assert.Equal(x, _startTime + TimeSpan.FromSeconds(20)));
        }

        [Fact]
        public void ExceptionHasMessage()
        {
            var ex = Assert.Throws<DelayTasksNotCompletedException>(() => _fakeAsync.Isolate(()
                 => Task.Delay(TimeSpan.FromSeconds(10))));

            Assert.Contains("One or many Delay tasks are still waiting for time:", ex.Message);
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

            Assert.Equal(ex.UtcNow, _startTime.AddSeconds(0.75));
            Assert.Collection(ex.DelayUntilTimes,
                x => Assert.Equal(x, _startTime + TimeSpan.FromSeconds(1)),
                x => Assert.Equal(x, _startTime + TimeSpan.FromSeconds(20)));
        }

        [Fact]
        public void CanceledDelaysAreNotShownInException()
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

            Assert.Equal(ex.UtcNow, _startTime);
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

                Assert.Empty(list);

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
        public void ZeroDelayIsCompleted()
        {
            var cts = new CancellationTokenSource();

            _fakeAsync.Isolate(() =>
            {
                var testing1 = Task.Delay(0);
                Assert.True(testing1.IsCompletedSuccessfully);

                var testing2 = Task.Delay(0, cts.Token);
                Assert.True(testing2.IsCompletedSuccessfully);

                var testing3 = Task.Delay(TimeSpan.Zero);
                Assert.True(testing3.IsCompletedSuccessfully);

                var testing4 = Task.Delay(TimeSpan.Zero, cts.Token);
                Assert.True(testing4.IsCompletedSuccessfully);

                return Task.CompletedTask;
            });
        }
    }
}
