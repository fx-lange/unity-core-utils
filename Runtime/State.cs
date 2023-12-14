using System;
using UnityEngine;

namespace CoreFx
{
    [Serializable]
    public class State<T>
    {
        [SerializeField] private T _value;

        public T Value
        {
            get => _value;
            private set => _value = value;
        }

        public event Action<T> ValueChangedEvent;

        public void SetValue(T value, bool silent = false)
        {
            Value = value;

            if (silent) return;

            ValueChangedEvent?.Invoke(Value);
        }

        public static implicit operator T(State<T> state) => state.Value;
    }
}