using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CoreFx
{
    public interface IService
    {
    }

    //service locator without interface abstraction
    public static class StrictServiceLocator
    {
        private static Dictionary<Type, IService> _services = new();

        public static void Register(IService service)
        {
            var type = service.GetType();
            if (!_services.TryAdd(type, service))
            {
                Debug.LogWarning($"Type {type} already registered -> overwriting");
                _services[type] = service;
            }
        }
        
        public static void Unregister(IService service)
        {
            var type = service.GetType();
            if (_services.Remove(type))
            {
                Debug.Log($"Type {type} unregistered");
            }
        }

        public static T Get<T>() where T : Object, IService
        {
            var type = typeof(T);
            if (!Application.isPlaying)
            {
                return Object.FindAnyObjectByType<T>();
            }
            
            if (_services.TryGetValue(type, out var service))
            {
                return service as T;
            }

            Debug.LogError($"No service of type {type}");
            return null;
        }
    }
}