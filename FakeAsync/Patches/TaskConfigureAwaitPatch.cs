using HarmonyLib;
using System.Threading.Tasks;

namespace FakeAsyncs
{
    [HarmonyPatch(typeof(Task), "SetContinuationForAwait")]
    class TaskSetContinuationForAwaitPatch
    {
        public static bool Prefix(ref bool continueOnCapturedContext)
        {
            if (FakeAsync.CurrentInstance != null)
            {
                // continuation goes to fake task scheduler instead thread pool
                continueOnCapturedContext = true;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Task), "UnsafeSetContinuationForAwait")]
    class TaskUnsafeSetContinuationForAwaitPatch
    {
        public static bool Prefix(ref bool continueOnCapturedContext)
        {
            if (FakeAsync.CurrentInstance != null)
            {
                // continuation goes to fake task scheduler instead thread pool
                continueOnCapturedContext = true;
            }

            return true;
        }
    }
}
