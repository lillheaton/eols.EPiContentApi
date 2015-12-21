using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
                    .Where(s => !IsProxy(s))
                    .SelectMany(s => s.GetTypes())
                    .Where(
                        s =>
                        s.IsClass
                        && s.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == itype))
                    .ToArray();

            foreach (var c in propertyConvertClasses)
            {
                var interfaceList = c.GetInterfaces().Where(s => s.IsGenericType && s.GetGenericTypeDefinition() == itype);
                list.Add(c, interfaceList.ToArray());
            }

            return list;
        }

        private static bool IsProxy(Assembly assembly)
        {
            try
            {
                var loc = assembly.Location;
                return false;
            }
            catch (Exception)
            {
                return true;
            }
        }
    }
}
