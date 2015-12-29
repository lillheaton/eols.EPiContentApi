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
using EPiServer.Logging;
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
            typeof(UrlPropertyConverter), typeof(ContentAreaPropertyConverter),
            typeof(ContentReferencePropertyConverter),
            typeof(PageReferencePropertyConverter),
            typeof(LinkItemCollectionPropertyConverter),
            typeof(BlockDataPropertyConverter), typeof(PageTypePropertyConverter)
        };

        /// <summary>
        /// Dictionary with all the assemblies converters
        /// </summary>
        private Dictionary<Type, Type[]> propertyConverters;
        
        private ContentSerializer()
        {
            this.Setup();
        }
        
        /// <summary>
        /// Get all property converters in all referenced assemblies
        /// </summary>
        private void Setup()
        {
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
        }

        /// <summary>
        /// Serialize a PageData object
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public ContentModel Serialize(PageData page)
        {
            var data = new ContentModel
            {
                ContentId = page.ContentLink.ID,
                Url = page.ContentLink.GetFriendlyUrl(),
                Name = page.Name,
                ContentTypeId = page.ContentTypeID,
                PageTypeName = page.PageTypeName,
                LanguageBranch = page.LanguageBranch,
                Content = ConvertToKeyValue(page, page.LanguageBranch)
            };
            
            return data;
        }
        
        /// <summary>
        /// Will take T obj where T is class. Will convert the obj and all its properties
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="locale"></param>
        /// <returns></returns>
        public Dictionary<string, object> ConvertToKeyValue<T>(T obj, string locale) where T : class
        {
            Dictionary<string, object> dict;

            // Check if obj is already a cached content
            if (IsCachedContent(obj, locale, out dict))
            {
                return dict;
            }
            
            // Obj is not cached, serialize object
            dict = GetProperties(obj).ToDictionary(prop => prop.PropertyInfo.Name, prop => ConvertProperty(prop.PropertyInfo, obj, locale));

            // If obj is IContent, cache the object
            if (obj is IContent)
            {
                ContentApiCacheManager.CacheObject(dict, (obj as IContent).ContentLink, locale);
            }

            return dict;
        }

        /// <summary>
        /// Will check if the object exist in the cache. If so then reflect the object to see if any property should not be cached
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="locale"></param>
        /// <param name="cachedDict"></param>
        /// <returns></returns>
        private bool IsCachedContent(object obj, string locale, out Dictionary<string, object> cachedDict)
        {
            cachedDict = null;

            // IContent is the only object that will be cached
            if (obj is IContent)
            {
                var content = obj as IContent;
                cachedDict = ContentApiCacheManager.GetObject<Dictionary<string, object>>(content.ContentLink, locale);
                if (cachedDict != null)
                {
                    // Add a key to dictionary so you can more easily see what's being cached
                    if (!cachedDict.ContainsKey("IsCachedContent"))
                    {
                        cachedDict.Add("IsCachedContent", true);
                    };

                    // Loop throug all properties and check if some properties don't want to cache their content
                    HandleCachedContent(content, cachedDict, locale);

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check all properties if some needs to be recached or not at all
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="cachedDict"></param>
        /// <param name="locale"></param>
        private void HandleCachedContent(IContent owner, Dictionary<string, object> cachedDict, string locale)
        {
            // Loop throug all properties and check if some properties don't want to cache their content
            foreach (var prop in GetProperties(owner))
            {
                if (prop.Attribute is ApiPropertyAttribute)
                {
                    var attr = prop.Attribute as ApiPropertyAttribute;
                    if (!attr.Cache && cachedDict.ContainsKey(prop.PropertyInfo.Name))
                    {
                        cachedDict[prop.PropertyInfo.Name] = ConvertProperty(prop.PropertyInfo, owner, locale);
                        continue;
                    }
                }


                if (typeof(ContentArea).IsAssignableFrom(prop.PropertyInfo.PropertyType))
                {
                    var contentArea = prop.PropertyInfo.GetValue(owner) as ContentArea;
                    if (contentArea == null)
                        continue;

                    cachedDict[prop.PropertyInfo.Name] = ConvertProperty(prop.PropertyInfo, owner, locale);
                }
                else if (typeof(PageReference).IsAssignableFrom(prop.PropertyInfo.PropertyType))
                {
                    var pageRef = prop.PropertyInfo.GetValue(owner) as PageReference;
                    if (pageRef == null)
                        continue;

                    var contentRefDict = ContentApiCacheManager.GetObject<Dictionary<string, object>>(pageRef, locale);
                    if (contentRefDict == null)
                    {
                        cachedDict[prop.PropertyInfo.Name] = ConvertProperty(prop.PropertyInfo, owner, locale);
                    }
                }
                else if (typeof(IContent).IsAssignableFrom(prop.PropertyInfo.PropertyType))
                {
                    var contentobj = prop.PropertyInfo.GetValue(owner) as IContent;
                    if (contentobj == null)
                        continue;

                    var contentDict = ContentApiCacheManager.GetObject<Dictionary<string, object>>(contentobj.ContentLink, locale);
                    if (contentDict == null)
                    {
                        cachedDict[prop.PropertyInfo.Name] = ConvertProperty(prop.PropertyInfo, owner, locale);
                    }
                }
            }
        }




        /// <summary>
        /// Note! Uses hevy reflection! Will check if there is any property converter for the type. If so then invoke property converter method
        /// </summary>
        /// <param name="propType"></param>
        /// <param name="owner"></param>
        /// <param name="locale"></param>
        /// <returns></returns>
        private object ConvertProperty(PropertyInfo propType, object owner, string locale)
        {
            // Check if there is any property converter for the property type
            var classes = propertyConverters.Where(s => s.Value.Any(c => c.GetGenericArguments()[0].IsAssignableFrom(propType.PropertyType))).ToArray();
            if (classes.Any())
            {
                var instance = Activator.CreateInstance(classes.First().Key); // Create instance of property converter
                var method = instance.GetType().GetMethod("Convert"); 
                return method.Invoke(instance, new[] { propType.GetValue(owner), owner, locale }); // Invoke convert method
            }

            return propType.GetValue(owner);
        }

        /// <summary>
        /// Get all the properties from T obj that have any of the specified "propertyAttributes"
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        private IEnumerable<PropertyInfoModel> GetProperties<T>(T obj) where T : class
        {
            PropertyInfo[] properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // Check if T obj is a castle object (most likely it will be)
            if (!IsProxy(obj))
            {
                // Get all the properties from T obj that have any of the specified "propertyAttributes"
                List<PropertyInfo> dynamicProperties =
                    obj.GetType()
                        .BaseType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(s => s.GetCustomAttributes().Any(a => propertyAttributes.Contains(a.GetType())))
                        .ToList();

                // Loop all properties with ApiPropertyAttribute and check if any has "Hide" == true
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
                
                return properties.Where(s => dynamicProperties.Any(p => p.Name == s.Name)).Select(s => new PropertyInfoModel { PropertyInfo = s, Attribute = s.GetCustomAttributes().OfType<ApiPropertyAttribute>().FirstOrDefault()} );
            }
            
            return properties.Where(s => s.GetCustomAttributes().Any(a => propertyAttributes.Contains(a.GetType()))).Select(s => new PropertyInfoModel { PropertyInfo = s, Attribute = s.GetCustomAttributes().OfType<ApiPropertyAttribute>().FirstOrDefault() });
        }

        /// <summary>
        /// Will check if the object is a proxy object (in EPiServers case Castle object)
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