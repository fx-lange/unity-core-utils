using System;
using System.Threading.Tasks;

namespace CoreFx.Extensions
{
    public static class TaskExtensions
    {
        public static async Task<bool> WaitUntil(this Func<bool> condition, int timeout = -1, int intervalMs = 33)
        {
            if (condition == null) throw new ArgumentNullException(nameof(condition));
            if (intervalMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(intervalMs), "Poll interval must be positive.");

            var waitTask = RunWaitLoop(condition, intervalMs);
            if (timeout < 0)
            {
                await waitTask;
                return true;
            }
            
            var timeoutTask = Task.Delay(timeout);
            var completedTask = await Task.WhenAny(waitTask, timeoutTask);
            return completedTask == waitTask;
        }

        private static async Task RunWaitLoop(Func<bool> condition, int intervalMs)
        {
            while (!condition())
            {
                await Task.Delay(intervalMs).ConfigureAwait(false);
            }
        }
    }
}