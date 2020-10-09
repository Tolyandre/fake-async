using HarmonyLib;
using System;

namespace FakeAsyncs
{
    [HarmonyPatch(typeof(DateTime), "get_UtcNow")]
    class DateTimeUtcNow
    {
        static DateTime Postfix(DateTime __result)
        {
            return FakeAsync.CurrentInstance?.Now.ToUniversalTime() ?? __result;
        }
    }
}
