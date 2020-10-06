using FakeTimes;
using HarmonyLib;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    public static class TaskTestsHelper
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
