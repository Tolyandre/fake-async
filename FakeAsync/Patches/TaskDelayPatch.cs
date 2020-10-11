﻿using HarmonyLib;
using System;
using System.Threading.Tasks;

namespace FakeAsyncs
{
    [HarmonyPatch(typeof(Task), "Delay", typeof(TimeSpan))]
    class TaskDelayPatch
    {
        static bool Prefix(ref Task __result, TimeSpan delay)
        {
            __result = FakeAsync.CurrentInstance?.CreateFakeDelay(delay);

            return __result == null;
        }
    }
}
