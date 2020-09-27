using HarmonyLib;
using System;

namespace FakeTime
{
    [HarmonyPatch(typeof(DateTime), "get_Now")]
    class DateTimeNowPatch
    {
        static DateTime Postfix(DateTime __result)
        {
            return new DateTime(2020, 9, 27);
        }
    }
}
