using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace FakeAsyncs
{
    //    [HarmonyPatch(typeof(TaskScheduler), "get_Default")]
    //    class TaskSchedulerPatch
    //    {
    //        static TaskScheduler Postfix(TaskScheduler __result)
    //        {
    //            return FakeAsync.CurrentInstance?.DeterministicTaskScheduler ?? __result;
    //        }
    //    }


    [HarmonyPatch(typeof(Task), "TaskConstructorCore")]
    class TaskCtorPatch
    {
        //IEnumerable<MethodBase> TargetMethods()
        //{
        //    // Searching for:
        //    // internal void TaskConstructorCore(Delegate? action, object? state, CancellationToken cancellationToken,
        //    // TaskCreationOptions creationOptions, InternalTaskOptions internalOptions, TaskScheduler? scheduler)
        //    var x = typeof(Task).GetMethods(AccessTools.all)
        //        .Where(methodInfo => methodInfo.Name == "TaskConstructorCore")
        //        .Where(methodInfo => methodInfo
        //            .GetParameters().Any(p => p.ParameterType == typeof(TaskScheduler))
        //        ).ToArray();
        //    return x;
        //}

        static void Prefix(ref TaskScheduler scheduler)
        {
            if (scheduler == TaskScheduler.Default)
            {
                var currentFakeTimeScheduler = FakeAsync.CurrentInstance?.DeterministicTaskScheduler;
                scheduler = currentFakeTimeScheduler ?? scheduler;
            }
        }
    }
}
