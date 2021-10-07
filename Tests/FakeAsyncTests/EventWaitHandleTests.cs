using FakeAsyncs;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FakeAsyncTests
{
    public class EventWaitHandleTests
    {
        private readonly FakeAsync _fakeAsync = new FakeAsync();

        [Fact]
        public void WaitOneTest()
        {
            var eventWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
            bool flag = false;

            _fakeAsync.Isolate(() =>
            {
                var testing = Task.Run(() =>
                {
                    eventWaitHandle.WaitOne(TimeSpan.FromSeconds(5));
                    flag = true;
                });

                _fakeAsync.Tick(TimeSpan.FromSeconds(4));
                Assert.False(flag);

                _fakeAsync.Tick(TimeSpan.FromSeconds(1));
                Assert.True(flag);

                return testing;
            });
        }
    }
}
