using System;
using UnityEngine;

namespace CoreFx.Extensions
{
    public static class AwaitableExtensions
    {
        public static Awaitable AwaitUntil(this Func<bool> condition, int timeout = -1, int intervalMs = 33)
        {
            if (condition == null) throw new ArgumentNullException(nameof(condition));
            if (intervalMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(intervalMs), "Poll interval must be positive.");

            var source = new AwaitableCompletionSource();

            if (condition())
            {
                source.SetResult();
                return source.Awaitable;
            }

            var interval = TimeSpan.FromMilliseconds(intervalMs);
            
            async void Poll()
            {
                while (!condition())
                {
                    await Awaitable.WaitForSecondsAsync((float)interval.TotalSeconds);
                }
                source.SetResult();
            }
            
            Poll();
            return source.Awaitable;
        }
    }
}