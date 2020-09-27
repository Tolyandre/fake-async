using HarmonyLib;
using System;
using System.Threading;

namespace FakeTime
{
    [HarmonyPatch(typeof(Thread), "Sleep", typeof(TimeSpan))]
    class ThreadSleepPatch
    {

        static bool Prefix()
        {
            return false;
        }
    }
}
