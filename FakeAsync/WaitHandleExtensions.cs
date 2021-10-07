using System.Runtime.CompilerServices;
using System.Threading;

namespace FakeAsyncs
{
    static class WaitHandleExtensions
    {
        private static readonly ConditionalWeakTable<WaitHandle, object> _usePatch = new ConditionalWeakTable<WaitHandle, object>();

        public static void SetSkipPatching(this WaitHandle waitHandle, bool skip)
        {
            _usePatch.Remove(waitHandle);
            _usePatch.Add(waitHandle, skip);
        }

        public static bool GetSkipPatching(this WaitHandle waitHandle)
        {
            if (_usePatch.TryGetValue(waitHandle, out object skip))
                return (bool)skip;

            return false;
        }
    }
}
