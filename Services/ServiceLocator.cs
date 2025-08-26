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
    }
}