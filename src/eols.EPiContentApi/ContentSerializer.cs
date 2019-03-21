using EOls.EPiContentApi.Attributes;
using EOls.EPiContentApi.Extensions;
using EOls.EPiContentApi.Models;
using EOls.EPiContentApi.Services.Cache;
using EOls.EPiContentApi.Services.Converter;
using EOls.EPiContentApi.Services.Reflection;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace EOls.EPiContentApi
{
    public static class ContentSerializer
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        private static readonly Injected<IContentTypeRepository> _contentTypeRepository = new Injected<IContentTypeRepository>();

        //private static readonly Injected<ICacheService> _cacheService = new Injected<ICacheService>();
        private static readonly Injected<IConverterService> _converterService = new Injected<IConverterService>();
        //private static readonly Injected<IReflectionService> _reflectionService = new Injected<IReflectionService>();

        private static IContentTypeRepository ContentTypeRepository { get => _contentTypeRepository.Service; }
        //private static ICacheService CacheService { get => _cacheService.Service; }
        private static IConverterService ConverterService { get => _converterService.Service;  }
        //private static IReflectionService ReflectionService { get => _reflectionService.Service; }

        /// <summary>
        /// List of attributes that will locate properties on IContent
        /// </summary>
        //private static readonly Type[] _propertyAttributes = { typeof(DisplayAttribute), typeof(ApiPropertyAttribute) };
        private static readonly Type[] _noneConvertableTypes = new[] { typeof(string), typeof(KeyValuePair<,>), typeof(Dictionary<,>) };


        static ContentSerializer()
        {
        }

        /// <summary>
        /// Serialize a PageData object
        /// </summary>
        /// <param name="page"></param>
        /// <param name="cacheRootLevel">If true, will cache whole object with cache key page reference id.</param>
        /// <returns></returns>
        public static object SerializePage(PageData page, bool cacheRootLevel = false)
        {
            //object data;

            if (ConverterService.HasConverter(page.GetType()))
            {
                //data = ConvertType(page.GetType(), page, page.Language.Name);
                return Serialize(page, page.Language.Name);
            }
            else
            {
                return new ContentModel
                {
                    ContentId = page.ContentLink.ID,
                    Url = page.ContentLink.GetFriendlyUrl(),
                    Name = page.Name,
                    ContentTypeId = page.ContentTypeID,
                    PageTypeName = page.PageTypeName,
                    LanguageBranch = page.Language.Name,
                    Content = Serialize(page, page.Language.Name)
                };
            }
            
            //if (cacheRootLevel) 
            //    CacheService.CacheObject(data, page.ContentLink, page.LanguageBranch);
            
            //return data;
        }

        // NEW STUFF

        private static bool IsClassObject(object targetObject)
        {
            if (targetObject == null)
                return false;

            var targetType = targetObject.GetType();
            return targetType.IsClass && targetType != typeof(string) && targetType.IsValueType == false;
        }

        private static List<T> Select<T>(this System.Collections.IEnumerable enumerable, Func<object, T> selector)
        {
            var enumerator = enumerable.GetEnumerator();
            var result = new List<T>();
            while (enumerator.MoveNext())
            {
                object current = enumerator.Current;
                result.Add(selector(current));
            }

            return result;
        }

        private static object InvokePropertyConverter(object propertyConverter, object target, object owner, string locale)
        {
            try
            {
                var method = propertyConverter.GetType().GetMethod("Convert");
                return method.Invoke(propertyConverter, new[] { target, owner, locale }); // Invoke convert method    
            }
            catch (Exception ex)
            {
                Logger.Error($"Error while triggering 'Convert' method in PropertyConverter {propertyConverter.GetType().Name} : {ex.Message}");
                throw;
            }
        }

        private static Dictionary<string, object> TransformClassObject(object targetObject, string locale)
        {
            var test = targetObject
                .GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.HasAttributeWithConditionOrTrue<ApiPropertyAttribute>(attr => attr.Hide == false))
                .ToArray();            

            return test
                .Select(propertyInfo => ConvertPropertyyyy(propertyInfo, targetObject, locale))
                .ToDictionary(x => x.Key, x => x.Value);
        }

        private static object TransformObject(object targetObject, string locale)
        {
            if(targetObject == null) return null;            

            var targetType = targetObject.GetType();
            
            switch (targetObject)
            {
                case IDictionary dictionary
                when targetType.IsGenericType
                    ? targetType.GetGenericTypeDefinition() == typeof(Dictionary<,>)
                    : targetType.GetElementType() == typeof(Dictionary<,>):

                    var keyType = dictionary.GetType().GetGenericArguments()[0];
                    if(keyType != typeof(string))
                        return dictionary;

                    var transformedDict = new Dictionary<string, object>();
                    var keysArr = dictionary.Keys.Cast<string>();
                    var valueArr = dictionary.Values.Cast<object>();

                    for (int i = 0; i < dictionary.Count; i++)
                    {
                        var currentValue = valueArr.ElementAt(i);

                        if (!IsClassObject(currentValue))
                        {
                            transformedDict.Add(keysArr.ElementAt(i), currentValue);                        
                        } else
                        {
                            transformedDict.Add(keysArr.ElementAt(i), TransformObject(valueArr.ElementAt(i), locale));
                        }
                    }
                    
                    return transformedDict;                    

                case IEnumerable enumerable 
                when (targetType.IsArray || (targetType.IsGenericType && targetObject is IEnumerable)
                    && IsClassObject(targetType.IsGenericType 
                        ? targetType.GetGenericTypeDefinition() 
                        : targetType.GetElementType()
                        )
                    ):
                    return enumerable.Select(target => TransformObject(target, locale));

                case object _ when ConverterService.TryFind(targetType, out object propertyConverter):
                    return InvokePropertyConverter(propertyConverter, targetObject, null, locale);

                case IContent content:
                    return Serialize(content, locale, false);

                case IContentData contentData:
                    return Serialize(contentData, locale);

                case object classObject when IsClassObject(targetObject):
                    return TransformClassObject(classObject, locale);
                    
                default:
                    return targetObject;
            }
        }

        private static KeyValuePair<string, object> ConvertPropertyyyy(PropertyInfo propTypeInfo, object owner, string locale)
        {
            try
            {
                object targetValue = propTypeInfo.GetValue(owner, null);
                return new KeyValuePair<string, object>(propTypeInfo.Name, TransformObject(targetValue, locale));
            }
            catch (Exception e)
            {
                return new KeyValuePair<string, object>(propTypeInfo.Name, owner);
            }
        }

        public static Dictionary<string, object> Serialize(IContent content, string locale, bool cacheRootLevel = false)
        {
            //object target = Serialize(content as object, locale);

            //if (cacheRootLevel)
            //    CacheService.CacheObject(target, content.ContentLink, locale);

            //return target;

            var contentType = ContentTypeRepository
                .Load(content.ContentTypeID);

            return contentType
                .PropertyDefinitions
                .Where(propDefinition => propDefinition.ExistsOnModel)
                .Select(propDefinition => contentType.ModelType.GetProperty(propDefinition.Name))                
                .Where(x => x.HasAttributeWithConditionOrTrue<ApiPropertyAttribute>(attr => attr.Hide == false))
                .Select(propertyInfo => ConvertPropertyyyy(propertyInfo, content, locale))
                .ToDictionary(x => x.Key, x => x.Value);
        }

        public static Dictionary<string, object> Serialize(IContentData contentData, string locale)
        {
            if(contentData is IContent content)
            {
                return Serialize(content, locale, false);
            }

            var test = contentData
                .GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.HasAttribute<DisplayAttribute>())
                .Where(x => x.HasAttributeWithConditionOrTrue<ApiPropertyAttribute>(attr => attr.Hide == false))
                .ToArray();

            return test
                .Select(propertyInfo => ConvertPropertyyyy(propertyInfo, contentData, locale))
                .ToDictionary(x => x.Key, x => x.Value);
        }

        // END NEW STUFF

        public static Dictionary<string, object> Serialize(object targetObject, string locale)
        {
            if (targetObject == null) return null;

            var targetType = targetObject.GetType();

            if(targetType.IsValueType || targetType == typeof(string))
            {
                throw new ArgumentException($"Can't serialize object of type {targetType.FullName}");
            }

            switch (targetObject)
            {
                case IContent content:
                    return Serialize(content, locale, false);

                default:
                    return TransformClassObject(targetObject, locale);
            }
            
            //return TransformObject(obj, locale);
            //object target = obj;

            //if (ConverterService.HasConverter(target.GetType()))
            //{
            //    target = ConvertType(target.GetType(), target, locale);
            //}
            //else
            //{
            //    target = ConvertToKeyValue(target, locale);
            //}

            //return target;
        }

        /// <summary>
        /// Will take T obj where T is class. Will convert the obj and all its properties
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="locale"></param>
        /// <returns></returns>
        //public static Dictionary<string, object> ConvertToKeyValue<T>(T obj, string locale) where T : class
        //{
        //    Dictionary<string, object> dict;

        //    // Check if obj is already a cached content
        //    if (IsCachedContent(obj, locale, out dict))
        //    {
        //        return dict;
        //    }
            
        //    // Obj is not cached, serialize object
        //    dict = GetNoneHiddenProperties(obj)
        //        .ToDictionary(
        //            prop => prop.Name, 
        //            prop => ConvertProperty(prop, obj, locale)
        //        );

        //    // If obj is IContent, cache the object
        //    if (obj is IContent)
        //    {
        //        CacheService.CacheObject(dict, (obj as IContent).ContentLink, locale);
        //    }

        //    return dict;
        //}

        //public static object GetCachedObject(ContentReference contentRef, string locale)
        //{
        //    return CacheService.GetObject<object>(contentRef, locale);
        //}


        /// <summary>
        /// Will check if the object exist in the cache. If so then reflect the object to see if any property should not be cached
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="locale"></param>
        /// <param name="cachedDict"></param>
        /// <returns></returns>
        //private static bool IsCachedContent(object obj, string locale, out Dictionary<string, object> cachedDict)
        //{
        //    cachedDict = null;

        //    // IContent is the only object that will be cached
        //    if (obj is IContent)
        //    {
        //        var content = obj as IContent;
        //        cachedDict = CacheService.GetObject<Dictionary<string, object>>(content.ContentLink, locale);
        //        if (cachedDict != null)
        //        {
        //            // Loop throug all properties and check if some properties don't want to cache their content
        //            HandleCachedContent(content, cachedDict, locale);

        //            return true;
        //        }
        //    }

        //    return false;
        //}

        /// <summary>
        /// Check all properties if some needs to be recached or not at all
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="cachedDict"></param>
        /// <param name="locale"></param>
        //private static void HandleCachedContent(IContent owner, Dictionary<string, object> cachedDict, string locale)
        //{
        //    // Loop throug all properties and check if some properties don't want to cache their content
        //    foreach (var prop in ReflectionService.GetPropertiesWithAttributes(owner, _propertyAttributes))
        //    {
        //        ApiPropertyAttribute attr = prop.Attributes.OfType<ApiPropertyAttribute>().FirstOrDefault();
        //        if (attr != null && !attr.Cache && cachedDict.ContainsKey(prop.PropertyInfo.Name))
        //        {
        //            cachedDict[prop.PropertyInfo.Name] = ConvertProperty(prop.PropertyInfo, owner, locale);
        //            continue;
        //        }

        //        if (typeof(ContentArea).IsAssignableFrom(prop.PropertyInfo.PropertyType))
        //        {
        //            var contentArea = prop.PropertyInfo.GetValue(owner) as ContentArea;
        //            if (contentArea == null)
        //                continue;

        //            cachedDict[prop.PropertyInfo.Name] = ConvertProperty(prop.PropertyInfo, owner, locale);
        //        }
        //        else if (typeof(PageReference).IsAssignableFrom(prop.PropertyInfo.PropertyType))
        //        {
        //            var pageRef = prop.PropertyInfo.GetValue(owner) as PageReference;
        //            if (pageRef == null)
        //                continue;

        //            var contentRefDict = CacheService.GetObject<Dictionary<string, object>>(pageRef, locale);
        //            if (contentRefDict == null)
        //            {                        
        //                cachedDict[prop.PropertyInfo.Name] = ConvertProperty(prop.PropertyInfo, owner, locale);
        //            }
        //        }
        //        else if (typeof(IContent).IsAssignableFrom(prop.PropertyInfo.PropertyType))
        //        {
        //            var contentobj = prop.PropertyInfo.GetValue(owner) as IContent;
        //            if (contentobj == null)
        //                continue;

        //            var contentDict = CacheService.GetObject<Dictionary<string, object>>(contentobj.ContentLink, locale);
        //            if (contentDict == null)
        //            {
        //                cachedDict[prop.PropertyInfo.Name] = ConvertProperty(prop.PropertyInfo, owner, locale);
        //            }
        //        }
        //    }
        //}        

        /// <summary>
        /// Note! Uses hevy reflection! Will check if there is any property converter for the type. If so then invoke property converter method
        /// </summary>
        /// <param name="propTypeInfo"></param>
        /// <param name="owner"></param>
        /// <param name="locale"></param>
        /// <returns></returns>
        //private static object ConvertProperty(PropertyInfo propTypeInfo, object owner, string locale)
        //{
        //    object propertyValue = propTypeInfo.GetValue(owner);

        //    if (propertyValue == null)
        //        return null;
            
        //    if (propTypeInfo.PropertyType.IsArray || (propertyValue is System.Collections.IEnumerable && propertyValue.GetType().IsGenericType))
        //    {
        //        Type targetType;
        //        if (propertyValue.GetType().IsGenericType)
        //        {
        //            targetType = propertyValue.GetType().GetGenericTypeDefinition();
        //        }
        //        else
        //        {
        //            targetType = propertyValue.GetType().GetElementType();
        //        }
                
        //        var enumerable = propertyValue as System.Collections.IEnumerable;
        //        if(enumerable != null && !_noneConvertableTypes.Contains(targetType))
        //        {
        //            var enumerator = enumerable.GetEnumerator();

        //            var result = new List<object>();
        //            while (enumerator.MoveNext())
        //            {
        //                object current = enumerator.Current;
        //                result.Add(Serialize(current, locale));
        //            }

        //            return result;
        //        }
        //    }

        //    // Check if there is any property converter for the property type            
        //    if (ConverterService.TryFind(propTypeInfo.PropertyType, out object propertyConverter))
        //    {
        //        try
        //        {
        //            var method = propertyConverter.GetType().GetMethod("Convert");
        //            return method.Invoke(propertyConverter, new[] {  propertyValue, owner, locale }); // Invoke convert method    
        //        }
        //        catch (Exception ex)
        //        {
        //            Logger.Error($"Error while triggering 'Convert' method in PropertyConverter {propertyConverter.GetType().Name} : {ex.Message}");
        //            throw;
        //        }
        //    }

        //    if (IsApiPropertyClass(propertyValue))
        //    {
        //        return Serialize(propertyValue, locale);
        //    }

        //    return propertyValue;
        //}

        //private static object ConvertType(Type target, object obj, string locale)
        //{            
        //    // Check if there is any property converter for the property type            
        //    if (ConverterService.TryFind(target, out object propertyConverter))
        //    {
        //        try
        //        {
        //            var method = propertyConverter.GetType().GetMethod("Convert");
        //            return method.Invoke(propertyConverter, new[] { obj, obj, locale }); // Invoke convert method    
        //        }
        //        catch (Exception ex)
        //        {
        //            Logger.Error($"Error while triggering 'Convert' method in PropertyConverter {propertyConverter.GetType().Name} : {ex.Message}");
        //            throw;
        //        }
        //    }

        //    return obj;
        //}

        ///// <summary>
        ///// Get all the none hidden properties from T obj that have any of the specified "propertyAttributes"
        ///// </summary>        
        ///// <param name="obj"></param>
        ///// <returns></returns>
        //private static IEnumerable<PropertyInfo> GetNoneHiddenProperties(object obj)
        //{
        //    // Class has ApiPropertyAttribute with hide on.
        //    bool hideClass = HideClass(obj);

        //    Type[] searchAttributes;
        //    if (hideClass)
        //    {
        //        searchAttributes = new[] { typeof(ApiPropertyAttribute) };
        //    }
        //    else
        //    {
        //        searchAttributes = _propertyAttributes;
        //    }

        //    return ReflectionService.GetPropertiesWithAttributes(obj, searchAttributes)
        //        .Where(x =>
        //        {
        //            var apiAttr = x.Attributes.OfType<ApiPropertyAttribute>().FirstOrDefault();
        //            if (apiAttr != null && apiAttr.Hide)
        //            {
        //                return false;
        //            }

        //            return true;
        //        })
        //        .Select(x => x.PropertyInfo);
        //}

        //private static bool HideClass(object obj)
        //{
        //    if (obj.GetType().IsClass)
        //    {
        //        var attr = obj.GetType().GetCustomAttributes<ApiPropertyAttribute>().FirstOrDefault();
        //        if(attr != null && attr.Hide)
        //        {
        //            return true;
        //        }
        //    }

        //    return false;
        //}

        //private static bool IsApiPropertyClass(object obj)
        //{
        //    if (obj.GetType().IsClass)
        //    {
        //        var attr = obj.GetType().GetCustomAttributes<ApiPropertyAttribute>().FirstOrDefault();
        //        if (attr != null && !attr.Hide)
        //        {
        //            return true;
        //        }
        //    }

        //    return false;
        //}
    }
}