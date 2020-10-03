using HarmonyLib;
using System;

namespace FakeTimes
{
    [HarmonyPatch(typeof(DateTime), "get_UtcNow")]
    class DateTimeUtcNow
    {
        static DateTime Postfix(DateTime __result)
        {
            return FakeTime.CurrentInstance?.Now.ToUniversalTime() ?? __result;
        }
    }
}
