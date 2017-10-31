using System;

namespace EOls.EPiContentApi.Services.Converter
{
    public interface IConverterService
    {
        object Find(Type converterType);
        bool TryFind(Type converterType, out object instance);
        bool HasConverter(Type converterType);
    }
}
