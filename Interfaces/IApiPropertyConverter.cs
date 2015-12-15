namespace EOls.EPiContentApi.Interfaces
{
    public interface IApiPropertyConverter<T>
    {
        object Convert(T obj, string locale);
    }
}