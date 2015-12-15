using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

using EOls.EPiContentApi.Attributes;
using EOls.EPiContentApi.Extensions;
using EOls.EPiContentApi.Interfaces;
using EOls.EPiContentApi.Models;
using EOls.EPiContentApi.Util;

using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;

namespace EOls.EPiContentApi
{
    // http://jondjones.com/dependency-injection-in-episerver-servicelocator-and-injected-explained/ maybe allow users to inject service

    public sealed class ContentSerializer
    {
        public static ContentSerializer Instance { get; } = new ContentSerializer();
        private static Type[] propertyAttributes = { typeof(DisplayAttribute), typeof(ApiPropertyAttribute) };

        private IContentRepository Repo { get; } = ServiceLocator.Current.GetInstance<IContentRepository>();
        private Dictionary<Type, Type[]> propertyConverters;

        private ContentSerializer()
        {
            propertyConverters = ReflectionHelper.GetAssemblyClassesInheritInterface(typeof(IApiPropertyConverter<>));
        }
        



        public ContentModel Serialize(PageData root)
        {
            return Convert(root);
        }

        private ContentModel Convert(PageData page)
        {
            return new ContentModel
            {
                ContentId = page.ContentLink.ID,
                Url = page.ContentLink.GetFriendlyUrl(),
                Name = page.Name,
                ContentTypeId = page.ContentTypeID,
                PageTypeName = page.PageTypeName,
                Content = ConvertToKeyValue(page, page.LanguageBranch).ToDictionary(s => s.Key, s => s.Value)
            };
        }

        private IEnumerable<KeyValuePair<string, object>> ConvertToKeyValue<T>(T obj, string locale) where T : class
        {
            foreach (var prop in GetProperties(obj))
            {
                yield return new KeyValuePair<string, object>(prop.Name, ConvertProperty(prop, obj, locale));
            }
        }
        
        private object ConvertProperty(PropertyInfo propType, object owner, string locale)
        {
            var classes = propertyConverters.Where(s => s.Value.Any(c => c.GetGenericArguments()[0] == propType.PropertyType)).ToArray();
            if (classes.Any())
            {
                var instance = Activator.CreateInstance(classes.First().Key);
                var method = instance.GetType().GetMethod("Convert");
                return method.Invoke(instance, new[] { propType.GetValue(owner), locale });
            }

            return propType.GetValue(owner);
        }



        private IEnumerable<PropertyInfo> GetProperties<T>(T obj) where T : class
        {
            PropertyInfo[] properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            
            if (IsProxy(obj))
            {
                PropertyInfo[] dynamicProperties =
                    obj.GetType()
                        .BaseType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(s => s.GetCustomAttributes().Any(a => propertyAttributes.Contains(a.GetType())))
                        .ToArray();

                return properties.Where(s => dynamicProperties.Any(p => p.Name == s.Name));
            }

            return properties.Where(s => s.GetCustomAttributes().Any(a => propertyAttributes.Contains(a.GetType()))).ToArray();
        }

        /// <summary>
        /// Will check if the object is a proxy object
        /// </summary>
        private static bool IsProxy(object obj)
        {
            try
            {
                string location = obj.GetType().Assembly.Location;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}