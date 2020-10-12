using FakeAsyncs;
using HarmonyLib;
using System.Threading.Tasks;
using Xunit;

namespace FakeAsyncTests
{
    public static class Helper
    {
        public static void AssertIfTheadPoolTaskScheduler(this Task task)
        {
            Assert.NotNull(task);

            var taskScheduler = Traverse.Create(task)
                .Field("m_taskScheduler")
                .GetValue();

            Assert.NotNull(taskScheduler);
            Assert.Equal("System.Threading.Tasks.ThreadPoolTaskScheduler", taskScheduler.GetType().FullName);
        }

        public static void AssertIfFakeTaskScheduler(this Task task)
        {
            Assert.NotNull(task);

            var taskScheduler = Traverse.Create(task)
                .Field("m_taskScheduler")
                .GetValue();

            Assert.NotNull(taskScheduler);
            Assert.Equal(typeof(DeterministicTaskScheduler).FullName, taskScheduler.GetType().FullName);
        }
    }
}
