using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using EOls.EPiContentApi.Attributes;
using EOls.EPiContentApi.Converters;
using EOls.EPiContentApi.Extensions;
using EOls.EPiContentApi.Interfaces;
using EOls.EPiContentApi.Models;
using EOls.EPiContentApi.Util;

using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.SpecializedProperties;

namespace EOls.EPiContentApi
{
    // http://jondjones.com/dependency-injection-in-episerver-servicelocator-and-injected-explained/ maybe allow users to inject service

    public sealed class ContentSerializer
    {
        private static object _lock = new object();
        private static ContentSerializer _instance;
        public static ContentSerializer Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ContentSerializer();                            
                        }
                    }
                }

                return _instance;
            }
        }

        /// <summary>
        /// List of attributes that will locate properties on IContent
        /// </summary>
        private static Type[] propertyAttributes = { typeof(DisplayAttribute), typeof(ApiPropertyAttribute) };

        /// <summary>
        /// List all the local project converters. This list can be overridden by the nuget user
        /// </summary>
        private Type[] eolsConverters =
        {
            typeof(UrlConverter), typeof(ContentAreaPropertyConverter),
            typeof(ContentReferencePropertyConverter),
            typeof(PageReferencePropertyConverter),
            typeof(LinkItemCollectionPropertyConverter)
        };

        /// <summary>
        /// Dictionary with all the assemblies converters
        /// </summary>
        private Dictionary<Type, Type[]> propertyConverters;

        private ContentSerializer()
        {
            this.Setup();
        }
        
        private void Setup()
        {
            var watch = new Stopwatch();
            watch.Start();

            // Get all asseblies converters
            propertyConverters = ReflectionHelper.GetAssemblyClassesInheritInterface(typeof(IApiPropertyConverter<>));

            // Override the built in converters
            int propConvertersCount = propertyConverters.Count;
            for (int i = propConvertersCount; i-- > 0;)
            {
                foreach (var eolsConverter in eolsConverters)
                {
                    if(eolsConverter == propertyConverters.ElementAt(i).Key) continue;

                    if (propertyConverters.ElementAt(i).Value.Any(propConvInterface => eolsConverter.GetInterfaces().Any(s => s.GetGenericArguments()[0] == propConvInterface.GetGenericArguments()[0])))
                    {
                        propertyConverters.Remove(eolsConverter);
                    }
                }
            }

            watch.Stop();            
        }
        
        public ContentModel Serialize(PageData root)
        {
            return ConvertPage(root);
        }

        public ContentModel ConvertPage(PageData page)
        {
            return new ContentModel
            {
                ContentId = page.ContentLink.ID,
                Url = page.ContentLink.GetFriendlyUrl(),
                Name = page.Name,
                ContentTypeId = page.ContentTypeID,
                PageTypeName = page.PageTypeName,
                LanguageBranch = page.LanguageBranch,
                Content = ConvertToKeyValue(page, page.LanguageBranch)
            };
        }
        
        public Dictionary<string, object> ConvertToKeyValue<T>(T obj, string locale) where T : class
        {
            var dict = new Dictionary<string, object>();
            foreach (var prop in GetProperties(obj))
            {
                dict.Add(prop.Name, ConvertProperty(prop, obj, locale));
            }

            return dict;
        }




        
        private object ConvertProperty(PropertyInfo propType, object owner, string locale)
        {
            var classes = propertyConverters.Where(s => s.Value.Any(c => c.GetGenericArguments()[0].IsAssignableFrom(propType.PropertyType))).ToArray();
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

            if (!IsProxy(obj))
            {
                List<PropertyInfo> dynamicProperties =
                    obj.GetType()
                        .BaseType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(s => s.GetCustomAttributes().Any(a => a is ApiPropertyAttribute || a is DisplayAttribute))
                        .ToList();

                var apiAttributeProperties = dynamicProperties.Where(s => s.GetCustomAttributes().Any(a => a is ApiPropertyAttribute)).ToArray();
                var count = apiAttributeProperties.Count();
                for (int i = count; i--> 0;)
                {
                    var attribute = apiAttributeProperties[i].GetCustomAttributes().OfType<ApiPropertyAttribute>().First();
                    if (attribute.Hide)
                    {
                        dynamicProperties.Remove(apiAttributeProperties[i]);
                    }
                }
                
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