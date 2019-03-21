using System;
using System.Linq;
using System.Reflection;

namespace EOls.EPiContentApi
{
    public static class PropertyInfoEx
    {
        public static TAttribute GetAttribute<TAttribute>(this PropertyInfo propertyInfo) where TAttribute : Attribute
        {
            return propertyInfo.GetCustomAttributes<TAttribute>(true).FirstOrDefault();
        }

        public static bool HasAttribute<TAttribute>(this PropertyInfo propertyInfo) where TAttribute : Attribute
        {
            return propertyInfo.GetAttribute<TAttribute>() != null;
        }

        public static bool HasAttributeWithConditionOrTrue<TAttribute>(this PropertyInfo propertyInfo, Func<TAttribute, bool> filter) where TAttribute : Attribute
        {
            var attribute = propertyInfo.GetAttribute<TAttribute>();
            if (attribute == null)
                return true;

            return filter(attribute);
        }
    }
}
