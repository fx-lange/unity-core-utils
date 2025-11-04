using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace CoreFx.Extensions
{
    public static class UnityEventExtensions
    {
        public static Task AsTask(this UnityEvent @event)
        {
            if (@event == null) throw new ArgumentNullException(nameof(@event));
            
            var tcs = new TaskCompletionSource<bool>();
            
            UnityAction handler = null;
            handler = () =>
            {
                @event.RemoveListener(handler);
                tcs.SetResult(true);
            };
            
            @event.AddListener(handler);
            return tcs.Task;
        }

        public static Task<T> AsTask<T>(this UnityEvent<T> @event)
        {
            if (@event == null) throw new ArgumentNullException(nameof(@event));

            var tcs = new TaskCompletionSource<T>();
            
            UnityAction<T> handler = null;
            handler = val =>
            {
                @event.RemoveListener(handler);
                tcs.SetResult(val);
            };
            
            @event.AddListener(handler);
            return tcs.Task;
        }

        public static Awaitable AsAwaitable(this UnityEvent @event)
        {
            if (@event == null) throw new ArgumentNullException(nameof(@event));

            var acs = new AwaitableCompletionSource();

            UnityAction handler = null;
            handler = () =>
            {
                @event.RemoveListener(handler);
                acs.SetResult();
            };
            
            @event.AddListener(handler);
            return acs.Awaitable;
        }

        public static Awaitable<T> AsAwaitable<T>(this UnityEvent<T> @event)
        {
            if (@event == null) throw new ArgumentNullException(nameof(@event));

            var acs = new AwaitableCompletionSource<T>();

            UnityAction<T> handler = null;
            handler = val =>
            {
                @event.RemoveListener(handler);
                acs.SetResult(val);
            };
            
            @event.AddListener(handler);
            return acs.Awaitable;
        }
    }
}