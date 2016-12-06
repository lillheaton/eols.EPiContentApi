namespace EOls.EPiContentApi.Interfaces
{
    public interface IApiPropertyConverter<T>
    {
        object Convert(ContentSerializer serializer, T obj, object owner, string locale);
    }
}