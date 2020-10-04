using HarmonyLib;
using System.Threading.Tasks;

namespace FakeTimes
{
    [HarmonyPatch(typeof(TaskScheduler), "get_Default")]
    class TaskSchedulerPatch
    {
        static TaskScheduler Postfix(TaskScheduler __result)
        {
            return FakeAsync.CurrentInstance?.DeterministicTaskScheduler ?? __result;
        }
    }
}
