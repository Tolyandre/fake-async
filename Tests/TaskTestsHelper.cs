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
            var taskSchedulerType = Traverse.Create(task)
                .Field("m_taskScheduler")
                .GetValue()
                .GetType();

            Assert.Equal("System.Threading.Tasks.ThreadPoolTaskScheduler", taskSchedulerType.FullName);
        }

        public static void AssertIfFakeTaskScheduler(this Task task)
        {
            var taskSchedulerType = Traverse.Create(task)
                .Field("m_taskScheduler")
                .GetValue()
                .GetType();

            Assert.Equal(typeof(DeterministicTaskScheduler).FullName, taskSchedulerType.FullName);
        }
    }
}
