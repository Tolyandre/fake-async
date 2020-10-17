using FakeAsyncs;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FakeAsyncTests
{
    public class FakeAsyncTests
    {
        private readonly FakeAsync _fakeAsync = new FakeAsync();

        [Fact]
        public void RunsSynchronousCode()
        {
            var flag = false;

            _fakeAsync.Isolate(() =>
            {
                flag = true;
                return Task.CompletedTask;
            });

            Assert.True(flag);
        }

        [Fact]
        public void RunsAsynchronousCode()
        {
            var flag1 = false;
            var flag2 = false;

            _fakeAsync.Isolate(() =>
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
        public void RunsPendingTaskBeforeReturning()
        {
            var flag = false;

            _fakeAsync.Isolate(() =>
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
        public void TickSkipsTime()
        {
            bool flag = false;
            _fakeAsync.UtcNow = new DateTime(2020, 10, 20);

            _fakeAsync.Isolate(async () =>
            {
                async Task MethodUnderTest()
                {
                    await Task.Delay(TimeSpan.FromSeconds(10));

                    flag = true;
                    Assert.Equal(new DateTime(2020, 10, 20, 0, 0, 10), DateTime.Now);

                    await Task.Delay(TimeSpan.FromSeconds(10));
                }

                var testing = MethodUnderTest();

                _fakeAsync.Tick(TimeSpan.FromSeconds(9));
                Assert.False(flag); // after 9s flag still false

                _fakeAsync.Tick(TimeSpan.FromSeconds(1));
                Assert.True(flag); // skip 1s more and now flag==true

                _fakeAsync.Tick(TimeSpan.FromSeconds(10)); // skip remaining time

                await testing; // propagate any exceptions
            });
        }

        [Fact]
        public void ThrowsIfNested()
        {
            _fakeAsync.Isolate(async () =>
            {
                var fakeAsync2 = new FakeAsyncs.FakeAsync();

                var exception = Assert.Throws<InvalidOperationException>(() =>
                {
                    fakeAsync2.Isolate(() => Task.CompletedTask);
                });

                Assert.Equal("FakeAsync calls can not be nested", exception.Message);
            });
        }

        [Fact]
        public void PropagatesExceptionFromAsync()
        {
            Assert.Throws<ApplicationException>(() => _fakeAsync.Isolate(async () =>
            {
                throw new ApplicationException("A message");
            }));
        }

        [Fact]
        public void PropagatesExceptionFromSync()
        {
            Assert.Throws<ApplicationException>(() => _fakeAsync.Isolate(() =>
            {
                throw new ApplicationException("A message");
            }));
        }

        [Fact]
        public void ThrowsIfPendingTaskStillInQueue()
        {
            Assert.Throws<DelayTasksNotCompletedException>(() => _fakeAsync.Isolate(async () =>
             {
                 var testing = Task.Delay(TimeSpan.FromSeconds(10));

                 _fakeAsync.Tick(TimeSpan.FromSeconds(9));

                 await testing;
             }));
        }
    }
}
