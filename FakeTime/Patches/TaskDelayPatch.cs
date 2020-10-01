using HarmonyLib;
using System;
using System.Threading.Tasks;

namespace FakeTimes
{
    [HarmonyPatch(typeof(Task), "Delay", typeof(TimeSpan))]
    class TaskDelayPatch
    {
        static bool Prefix(ref Task __result, TimeSpan delay)
        {
            __result = FakeTime.CurrentTime.FakeDelay(delay);

            return false;
        }
    }
}
