using HarmonyLib;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FakeAsyncs
{
    [HarmonyPatch(typeof(Task), "Delay", typeof(int), typeof(CancellationToken))]
    class TaskDelayPatch
    {
        static Task Postfix(Task __result, int millisecondsDelay, CancellationToken cancellationToken)
        {
            return FakeAsync.CurrentInstance?.DecorateTaskDelay(__result, TimeSpan.FromMilliseconds(millisecondsDelay), cancellationToken)
                ?? __result;
        }
    }
}
