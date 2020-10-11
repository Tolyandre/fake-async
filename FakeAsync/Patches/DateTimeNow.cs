using HarmonyLib;
using System;

namespace FakeAsyncs
{
    [HarmonyPatch(typeof(DateTime), "get_Now")]
    class DateTimeNow
    {
        static DateTime Postfix(DateTime __result)
        {
            return FakeAsync.CurrentInstance?.UtcNow ?? __result;
        }
    }
}
