using HarmonyLib;
using System;
using System.Threading;

namespace FakeAsyncs
{
    [HarmonyPatch(typeof(WaitHandle), "WaitOneNoCheck", typeof(int))]
    class WaitHandlePatch
    {
        public static bool Prefix(ref int millisecondsTimeout, WaitHandle __instance, ref bool __result)
        {
            var fakeAsync = FakeAsync.CurrentInstance;

            if (fakeAsync == null || __instance.GetSkipPatching())
                return false; // false to invoke original method

            var manualResetEvent = new ManualResetEvent(false);
            var timeoutTask = fakeAsync.CreateDelayTask(TimeSpan.FromMilliseconds(millisecondsTimeout), CancellationToken.None)
                .ContinueWith((_, state) => {
                    ((ManualResetEvent)state).Set();
                }, manualResetEvent);

            var fakeAsyncSynchronizationContext = SynchronizationContext.Current as FakeAsyncSynchronizationContext;

            WaitHandle.WaitAny(new WaitHandle[] { __instance, manualResetEvent });
            manualResetEvent.Dispose();

            __result = !timeoutTask.IsCompleted;

            return true; // true to not invoke original method
        }
    }
}
