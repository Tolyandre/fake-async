using HarmonyLib;
using System;
using System.Threading.Tasks;

namespace FakeTime
{
    [HarmonyPatch(typeof(Task), "Delay", typeof(TimeSpan))]
    class TaskDelayPatch
    {

        static bool Prefix(ref Task __result, TimeSpan delay)
        {
            __result = Task.CompletedTask;

            return false;
        }
    }
}
