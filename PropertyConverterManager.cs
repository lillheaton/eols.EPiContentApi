using System;
using System.Collections.Generic;
using System.Linq;

using EOls.EPiContentApi.Converters;
using EOls.EPiContentApi.Interfaces;

namespace EOls.EPiContentApi
{
    public class PropertyConverterManager : IPropertyConverterManager
    {
        private Dictionary<Type, object> Converters { get; }

        public PropertyConverterManager()
        {
            Converters = new Dictionary<Type, object>();

            // Setup built-in converters
            this.AddConverter(new UrlPropertyConverter());
            this.AddConverter(new ContentAreaPropertyConverter());
            this.AddConverter(new ContentReferencePropertyConverter());
            this.AddConverter(new LinkItemCollectionPropertyConverter());
            this.AddConverter(new BlockDataPropertyConverter());
            this.AddConverter(new PageTypePropertyConverter());
        }

        /// <summary>
        /// Add IApiPropertyConverter. If it already exist it will replace it
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="converter"></param>
        public void AddConverter<T>(IApiPropertyConverter<T> converter)
        {
            // Does not exist in the dictionary, add it
            this.Converters.Add(typeof(T), converter);
        }

        /// <summary>
        /// Replace a specific converter
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="converter"></param>
        public void ReplaceConverter<T>(IApiPropertyConverter<T> converter)
        {
            this.Converters[typeof(T)] = converter;
        }

        /// <summary>
        /// Finds the converter, if not return null
        /// </summary>
        /// <returns></returns>
        public object Find(Type converterType)
        {
            // Get all converters that might match
            var converters = this.Converters.Where(s => s.Key == converterType || s.Key.IsAssignableFrom(converterType)).ToArray();

            // Check if there is multiple converters due to IsAssignableFrom, choose the one that has same type name
            var specificConverter = converters.Where(s => s.Key.Name == converterType.Name).Select(s => s.Value).FirstOrDefault();

            // If found specific converter or choose first in list || null
            return specificConverter ?? converters.Select(s => s.Value).FirstOrDefault();
        }

        /// <summary>
        /// Removes the converter from the list
        /// </summary>
        /// <param name="converterType"></param>
        public void Remove(Type converterType)
        {
            this.Converters.Remove(converterType);
        }
    }
}