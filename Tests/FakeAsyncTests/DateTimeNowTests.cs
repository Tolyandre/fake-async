using FakeAsyncs;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FakeAsyncTests
{
    public class DateTimeNowTests
    {
        [Fact]
        public void ConvertsLocalTimeToUtc()
        {
            var dateTime = new DateTime(2020, 10, 20);

            var fakeAsync = new FakeAsync
            {
                UtcNow = dateTime,
            };

            Assert.Equal(DateTimeKind.Utc, fakeAsync.UtcNow.Kind);
            Assert.Equal(dateTime.ToUniversalTime(), fakeAsync.UtcNow);
        }

        [Fact]
        public void DateTimeNowIsChanged()
        {
            var fakeAsync = new FakeAsync
            {
                UtcNow = new DateTime(2020, 9, 27, 0, 0, 0, DateTimeKind.Utc),
            };

            fakeAsync.Isolate(() =>
            {
                Assert.Equal(new DateTime(2020, 9, 27, 0, 0, 0, DateTimeKind.Utc), DateTime.Now.ToUniversalTime());
                Assert.Equal(new DateTime(2020, 9, 27, 0, 0, 0, DateTimeKind.Utc), DateTime.UtcNow);

                return Task.CompletedTask;
            });
        }

        [Fact]
        public void DateTimeKindIsSet()
        {
            var fakeAsync = new FakeAsync
            {
                UtcNow = new DateTime(2020, 10, 17, 0, 0, 0, DateTimeKind.Utc),
            };

            fakeAsync.Isolate(() =>
            {
                Assert.Equal(DateTimeKind.Local, DateTime.Now.Kind);
                Assert.Equal(DateTimeKind.Utc, DateTime.UtcNow.Kind);

                return Task.CompletedTask;
            });
        }

        [Fact]
        public void DateTimeDefaultValue()
        {
            var fakeAsync = new FakeAsync();

            fakeAsync.Isolate(() =>
            {
                Assert.Equal(new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToLocalTime(), DateTime.Now);
                Assert.Equal(new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc), DateTime.UtcNow);

                return Task.CompletedTask;
            });
        }

        [Fact]
        public void NowIsDefault()
        {
            var fakeAsync = new FakeAsync();

            Assert.Equal(default, fakeAsync.UtcNow);
            fakeAsync.Isolate(() =>
            {
                Assert.Equal(default, DateTime.UtcNow);
                Assert.Equal(default(DateTime).ToLocalTime(), DateTime.Now);

                return Task.CompletedTask;
            });
        }

        [Fact]
        public void TickUpdatesDateTimeNow()
        {
            var fakeAsync = new FakeAsync
            {
                UtcNow = new DateTime(2020, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            };

            fakeAsync.Isolate(() =>
            {
                fakeAsync.Tick(TimeSpan.FromDays(19));
                Assert.Equal(new DateTime(2020, 10, 20, 0, 0, 0, DateTimeKind.Utc), DateTime.UtcNow);

                fakeAsync.Tick(new TimeSpan(0, 17, 0, 30, 15));
                Assert.Equal(new DateTime(2020, 10, 20, 17, 0, 30, 15, DateTimeKind.Utc), DateTime.UtcNow);

                return Task.CompletedTask;
            });
        }

    }
}
