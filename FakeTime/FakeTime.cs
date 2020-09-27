using HarmonyLib;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FakeTime
{
    public static class FakeTime
    {
        internal static AsyncLocal<Time> CurrentTime { get; private set; } = new AsyncLocal<Time>();

        public static async Task Isolate(Func<Time, Task> methodUnderTest, CancellationToken cancellationToken = default)
        {
            if (CurrentTime.Value != null)
                throw new InvalidOperationException("Cannot run isolated test inside another isolated test");

            // id should be in reverse domain notation
            // https://harmony.pardeike.net/articles/basics.html#creating-a-harmony-instance
            const string harmonyId = "com.github.Tolyandre.fake-time";
            var harmony = new Harmony(harmonyId);

            harmony.PatchAll(typeof(FakeTime).Assembly);

            var time = new Time();
            CurrentTime.Value = time;

            try
            {
                await Task.Yield();
                await methodUnderTest(time);
            }
            finally
            {
                CurrentTime.Value = null;
                harmony.UnpatchAll(harmonyId);
            }
        }
    }

    public class Time
    {
        public DateTime FakeNow { get; private set; } = DateTime.Now;

        public void Flush()
        {

        }
    }
}
