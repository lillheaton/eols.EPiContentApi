using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EOls.EPiContentApi.Services.Reflection
{
    public interface IReflectionService
    {
        IEnumerable<Type> GetAssemblyClassesInheritGenericInterface(Type type, Assembly assembly);
        IEnumerable<Type> GetAssemblyClassesInheritGenericInterface(Type type, IEnumerable<Assembly> assemblies);
        Type GetGenericTypeOfInterface(Type type);

        IEnumerable<PropertyInfo> GetProperties(object obj, IEnumerable<Type> attributes);
        IEnumerable<(PropertyInfo PropertyInfo, Attribute[] Attributes)> GetPropertiesWithAttributes(object obj, IEnumerable<Type> attributes);        
    }
}
