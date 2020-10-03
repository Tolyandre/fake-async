using HarmonyLib;
using System;

namespace FakeTimes
{
    [HarmonyPatch(typeof(DateTime), "get_Now")]
    class DateTimeNow
    {
        static DateTime Postfix(DateTime __result)
        {
            return FakeTime.CurrentInstance?.Now ?? __result;
        }
    }
}
