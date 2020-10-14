using FakeAsyncs;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FakeAsyncTests
{
    public class CancelDelayTests
    {
        private readonly FakeAsync _fakeAsync = new FakeAsync
        {
            UtcNow = new DateTime(2020, 10, 20),
        };

        [Fact]
        public void DelayWithCanceledTokenIsCanceled()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.Throws<TaskCanceledException>(() =>
            _fakeAsync.Isolate(() =>
            {
                var testing = Task.Delay(1000, cts.Token);

                Assert.True(testing.IsCanceled);

                return testing;
            }));

            Assert.Throws<TaskCanceledException>(() =>
            _fakeAsync.Isolate(() =>
            {
                var testing = Task.Delay(TimeSpan.FromSeconds(1), cts.Token);

                Assert.True(testing.IsCanceled);

                return testing;
            }));
        }

        [Fact]
        public void ZeroDelayIsCanceled()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();

            _fakeAsync.Isolate(() =>
            {
                var testing1 = Task.Delay(0, cts.Token);
                var testing2 = Task.Delay(TimeSpan.Zero, cts.Token);

                Assert.True(testing1.IsCanceled);
                Assert.True(testing2.IsCanceled);

                return Task.CompletedTask;
            });
        }

        [Fact]
        public void CancelingDelayMakesItCanceled()
        {
            Assert.Throws<TaskCanceledException>(() =>
                _fakeAsync.Isolate(() =>
                {
                    var cts = new CancellationTokenSource();
                    var testing = Task.Delay(1000, cts.Token);

                    _fakeAsync.Tick(TimeSpan.FromSeconds(0.5));
                    cts.Cancel();

                    Assert.True(testing.IsCanceled);
                    return testing;
                }));

            Assert.Throws<TaskCanceledException>(() =>
             _fakeAsync.Isolate(() =>
             {
                 var cts = new CancellationTokenSource();
                 var testing = Task.Delay(TimeSpan.FromSeconds(1), cts.Token);

                 _fakeAsync.Tick(TimeSpan.FromSeconds(0.5));
                 cts.Cancel();

                 Assert.True(testing.IsCanceled);
                 return testing;
             }));
        }

        [Fact]
        public void DiscardedCanceledDelayDoesNotThrow()
        {
            _fakeAsync.Isolate(() =>
            {
                var cts = new CancellationTokenSource();

                _ = Task.Delay(1000, cts.Token);
                _ = Task.Delay(TimeSpan.FromDays(1), cts.Token);

                cts.Cancel();

                return Task.CompletedTask;
            });
        }

        [Fact]
        public void ExecutesContinueWith()
        {
            _fakeAsync.Isolate(() =>
            {
                var cts = new CancellationTokenSource();
                var flag1 = false;
                var flag2 = false;
                var flag3 = false;

                Task.Delay(1000, cts.Token)
                    .ContinueWith(_ => flag1 = true);

                Task.Delay(TimeSpan.FromSeconds(1), cts.Token)
                    .ContinueWith(_ => flag2 = true);

                Task.Delay(0, cts.Token)
                    .ContinueWith(_ => flag3 = true);

                Assert.False(flag1);
                Assert.False(flag2);
                Assert.False(flag3);

                cts.Cancel();

                Assert.True(flag1);
                Assert.True(flag2);
                Assert.True(flag3);

                return Task.CompletedTask;
            });
        }

        [Fact]
        public void AwaitingCanceledDelayWillNotContinue()
        {
            var flag = false;
            var cts = new CancellationTokenSource();

            async Task Method()
            {
                await Task.Delay(500, cts.Token);
                flag = true;
            }

            Assert.Throws<TaskCanceledException>(() => _fakeAsync.Isolate(() =>
             {
                 var testing = Method();

                 cts.Cancel();
                 _fakeAsync.Tick(TimeSpan.FromMilliseconds(1000));

                 Assert.False(flag);

                 return testing;
             }));
        }

        [Fact]
        public void AwaitingZeroCanceledDelayWillNotContinue()
        {
            var flag = false;
            var cts = new CancellationTokenSource();
            cts.Cancel();

            async Task Method()
            {
                await Task.Delay(0, cts.Token);
                flag = true;
            }

            Assert.Throws<TaskCanceledException>(() => _fakeAsync.Isolate(() =>
            {
                var testing = Method();

                _fakeAsync.Tick(TimeSpan.FromMilliseconds(1000));

                Assert.False(flag);

                return testing;
            }));
        }

        [Fact]
        public void TaskCanceledExceptionHasDefaultMessage()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();

            var ex = Assert.Throws<TaskCanceledException>(() => _fakeAsync.Isolate(() => Task.Delay(1, cts.Token)));

            Assert.Equal("A task was canceled.", ex.Message);
        }
    }
}
