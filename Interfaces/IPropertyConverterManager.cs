using System;

namespace EOls.EPiContentApi.Interfaces
{
    public interface IPropertyConverterManager
    {
        void AddConverter<T>(IApiPropertyConverter<T> converter);
        void ReplaceConverter<T>(IApiPropertyConverter<T> converter);
        object Find(Type converterType);
        void Remove(Type converterType);
    }
}
