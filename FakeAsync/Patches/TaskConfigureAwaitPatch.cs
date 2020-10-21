using HarmonyLib;
using System.Threading.Tasks;

namespace FakeAsyncs
{
    [HarmonyPatch(typeof(Task), "ConfigureAwait")]
    class TaskConfigureAwaitPatch
    {
        public static bool Prefix(ref bool continueOnCapturedContext)
        {
            continueOnCapturedContext = true;
            return true;
        }
    }
}
