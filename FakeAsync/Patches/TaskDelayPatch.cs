using HarmonyLib;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FakeAsyncs
{
    [HarmonyPatch(typeof(Task), "Delay", typeof(int), typeof(CancellationToken))]
    class TaskDelayPatch
    {
        public static bool Prefix(ref Task __result, int millisecondsDelay, CancellationToken cancellationToken)
        {
            __result = FakeAsync.CurrentInstance?.CreateDelayTask(TimeSpan.FromMilliseconds(millisecondsDelay), cancellationToken);
                
            return __result == null;
        }
    }
}
