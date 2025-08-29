using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrimeAppBooks.Services
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();

        public static void RegisterSingleton<TService>(TService instance)
        {
            _services[typeof(TService)] = instance;
        }

        public static TService GetService<TService>()
        {
            if (_services.TryGetValue(typeof(TService), out var service))
            {
                return (TService)service;
            }

            throw new InvalidOperationException($"Service {typeof(TService)} not registered");
        }

        public static bool TryGetService<TService>(out TService? service)
        {
            if (_services.TryGetValue(typeof(TService), out var serviceObj))
            {
                service = (TService)serviceObj;
                return true;
            }

            service = default;
            return false;
        }

        public static void ClearServices()
        {
            _services.Clear();
        }

        public static bool IsServiceRegistered<TService>()
        {
            return _services.ContainsKey(typeof(TService));
        }
    }
}