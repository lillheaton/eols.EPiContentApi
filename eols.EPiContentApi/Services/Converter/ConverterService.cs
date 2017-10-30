using EOls.EPiContentApi.Interfaces;
using EOls.EPiContentApi.Services.Reflection;
using EPiServer.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EOls.EPiContentApi.Services.Converter
{
    [ServiceConfiguration(ServiceType = typeof(IConverterService), Lifecycle = ServiceInstanceScope.Singleton)]
    public class ConverterService : IConverterService
    {
        private readonly IReflectionService _reflectionService;
        private readonly (object ConverterInstance, Type PropertyType)[] _propertyConverters;

        public ConverterService() : this(ServiceLocator.Current.GetInstance<IReflectionService>())
        {
        }
        
        public ConverterService(IReflectionService reflectionService)
        {
            _reflectionService = reflectionService;

            // Fetch all local assembly ApiPropertyConverters
            var localAssemblyConverters = 
                GetPropertyConverterTypes(new[] { this.GetType().Assembly })
                .Select(ApiPropertyConverter);

            // Fetch all assemblies ApiPropertyConverters apart from "this"
            var domainAssemblyConverters = 
                GetPropertyConverterTypes(
                    AppDomain.CurrentDomain.GetAssemblies()
                    .Where(x => x != this.GetType().Assembly)
                ).Select(ApiPropertyConverter);

            // Merge converters - Allow domain converters to override
            _propertyConverters = 
                domainAssemblyConverters
                .Concat(
                    localAssemblyConverters
                    .Where(x => !domainAssemblyConverters.Any(s => s.PropertyType != x.PropertyType))
                )
                .ToArray();

            (object ConverterInstance, Type PropertyType) ApiPropertyConverter(Type type) =>
                (Activator.CreateInstance(type), _reflectionService.GetGenericTypeOfInterface(type));
        }

        private Type[] GetPropertyConverterTypes(IEnumerable<Assembly> assemblies) =>
            _reflectionService.GetAssemblyClassesInheritGenericInterface(typeof(IApiPropertyConverter<>), assemblies).ToArray();
        
        /// <summary>
        /// Find converter in structure map, if not throw NotImplementedException
        /// </summary>
        /// <returns></returns>
        public object Find(Type convertingType)
        {
            if (!TryFind(convertingType, out object instance))
            {
                throw new NotImplementedException($"Could not find property converter with type: {convertingType.FullName}");
            }

            return instance;
        }

        /// <summary>
        /// Try find converter in structure map
        /// </summary>
        /// <param name="convertingType"></param>
        /// <param name="instance"></param>
        /// <returns></returns>
        public bool TryFind(Type convertingType, out object instance)
        {
            instance = _propertyConverters
                .FirstOrDefault(
                    x => x.PropertyType == convertingType || 
                    x.PropertyType.IsAssignableFrom(convertingType)
                )
                .ConverterInstance;

            return instance != null;
        }

        public bool HasConverter(Type converterType)
        {
            return TryFind(converterType, out object instance);
        }
    }
}