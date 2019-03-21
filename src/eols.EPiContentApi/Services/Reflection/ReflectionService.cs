using EPiServer;
using EPiServer.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EOls.EPiContentApi.Services.Reflection
{
    [ServiceConfiguration(ServiceType = typeof(IReflectionService), Lifecycle = ServiceInstanceScope.Singleton)]
    public class ReflectionService : IReflectionService
    {
        /// <summary>
        /// Get all classes that inherit interface type in assembly
        /// </summary>
        /// <typeparam name="type"></typeparam>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public IEnumerable<Type> GetAssemblyClassesInheritGenericInterface(Type type, Assembly assembly)
        {
            try
            {
                return
                assembly
                .GetTypes()
                .Where(
                    x => x.IsClass &&
                    x.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == type));
            }
            catch (Exception e)
            {
                return new Type[0];
            }
        }

        /// <summary>
        /// Get all classes that inherit interface type in assemblies
        /// </summary>
        /// <typeparam name="type"></typeparam>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public IEnumerable<Type> GetAssemblyClassesInheritGenericInterface(Type type, IEnumerable<Assembly> assemblies)
        {
            return assemblies.SelectMany(x => GetAssemblyClassesInheritGenericInterface(type, x));
        }

        /// <summary>
        /// Gets generic type of interface<T>
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public Type GetGenericTypeOfInterface(Type type)
        {
            return type.GetInterfaces().FirstOrDefault(i => i.IsGenericType)?.GetGenericArguments()[0];
        }
        
        public IEnumerable<PropertyInfo> GetProperties(object obj, IEnumerable<Type> attributes)
        {
            var originalType = obj.GetOriginalType();
            PropertyInfo[] properties = originalType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            return properties
                .Where(prop =>
                    prop
                    .GetCustomAttributes()
                    .Any(a =>
                        attributes.Any(x => x.Equals(a.GetType()))
                    )
                );
        }

        public IEnumerable<(PropertyInfo PropertyInfo, Attribute[] Attributes)> GetPropertiesWithAttributes(object obj, IEnumerable<Type> attributes)
        {
            var originalType = obj.GetOriginalType();
            PropertyInfo[] properties = originalType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            return GetProperties(obj, attributes).Select(s => 
                    (
                        s,
                        s.GetCustomAttributes().Where(x => attributes.Any(y => y.Equals(x.GetType()))).ToArray()
                    )
                );
        }        
    }
}
