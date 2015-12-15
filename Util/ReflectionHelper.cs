using System;
using System.Collections.Generic;
using System.Linq;

namespace EOls.EPiContentApi.Util
{
    public class ReflectionHelper
    {
        public static Dictionary<Type, Type[]> GetAssemblyClassesInheritInterface(Type itype)
        {
            if (!itype.IsInterface && !itype.IsGenericType)
            {
                throw new Exception("Type is not a interface or a generic");
            }

            var list = new Dictionary<Type, Type[]>();

            var propertyConvertClasses =
                AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(s => s.GetTypes())
                    .Where(
                        s =>
                        s.IsClass
                        && s.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == itype))
                    .ToArray();

//            var asfn = propertyConvertClasses.ToArray()[0].GetInterfaces()[0].GetGenericArguments()[0].IsPrimitive;

            foreach (var c in propertyConvertClasses)
            {
                var interfaceList = c.GetInterfaces().Where(s => s.IsGenericType && s.GetGenericTypeDefinition() == itype);
                list.Add(c, interfaceList.ToArray());
            }

            return list;
        }
    }
}
