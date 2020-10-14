using HarmonyLib;
using System.Threading.Tasks;

namespace FakeAsyncs
{
    [HarmonyPatch(typeof(Task), "TaskConstructorCore")]
    class TaskCtorPatch
    {
        public static void Prefix(ref TaskScheduler scheduler)
        {
            if (scheduler == TaskScheduler.Default)
            {
                var currentFakeAsyncTaskScheduler = FakeAsync.CurrentInstance?.DeterministicTaskScheduler;
                scheduler = currentFakeAsyncTaskScheduler ?? scheduler;
            }
        }
    }
}
