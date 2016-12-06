using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

using EOls.EPiContentApi.Attributes;
using EOls.EPiContentApi.Extensions;
using EOls.EPiContentApi.Interfaces;
using EOls.EPiContentApi.Models;

using EPiServer.Core;
using EPiServer.ServiceLocation;

namespace EOls.EPiContentApi
{
    public class ContentSerializer
    {
        private ICacheManager CacheManager { get; }
        private IPropertyConverterManager PropertyConverterManager { get; }
        
        /// <summary>
        /// List of attributes that will locate properties on IContent
        /// </summary>
        private static Type[] propertyAttributes = { typeof(DisplayAttribute), typeof(ApiPropertyAttribute) };
        
        public ContentSerializer()
        {
            this.CacheManager = ServiceLocator.Current.GetInstance<ICacheManager>();
            this.PropertyConverterManager = ServiceLocator.Current.GetInstance<IPropertyConverterManager>();
        }

        /// <summary>
        /// Serialize a PageData object
        /// </summary>
        /// <param name="page"></param>
        /// <param name="cacheRootLevel">If true, will cache whole object with cache key page reference id.</param>
        /// <returns></returns>
        public object Serialize(PageData page, bool cacheRootLevel = false)
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

            if (cacheRootLevel) 
                this.CacheManager.CacheObject(data, page.ContentLink, page.LanguageBranch);
            
            return data;
        }
        public object Serialize(IContent content, string locale, bool cacheRootLevel = false)
        {
            var dict = ConvertToKeyValue(content, locale);

            if (cacheRootLevel)
                this.CacheManager.CacheObject(dict, content.ContentLink, locale);
            
            return dict;
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
                this.CacheManager.CacheObject(dict, (obj as IContent).ContentLink, locale);
            }

            return dict;
        }

        public object GetCachedObject(ContentReference contentRef, string locale)
        {
            return this.CacheManager.GetObject<object>(contentRef, locale);
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
                cachedDict = this.CacheManager.GetObject<Dictionary<string, object>>(content.ContentLink, locale);
                if (cachedDict != null)
                {
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

                    var contentRefDict = this.CacheManager.GetObject<Dictionary<string, object>>(pageRef, locale);
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

                    var contentDict = this.CacheManager.GetObject<Dictionary<string, object>>(contentobj.ContentLink, locale);
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
            var propertyConverter = this.PropertyConverterManager.Find(propType.PropertyType);
            if (propertyConverter != null)
            {
                var method = propertyConverter.GetType().GetMethod("Convert"); 
                return method.Invoke(propertyConverter, new[] { this, propType.GetValue(owner), owner, locale }); // Invoke convert method
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
                for (int i = dynamicProperties.Count; i--> 0; i--)
                {
                    var attr = dynamicProperties[i].GetCustomAttributes().OfType<ApiPropertyAttribute>().FirstOrDefault();
                    if (attr != null && attr.Hide)
                    {
                        dynamicProperties.Remove(dynamicProperties[i]);
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