# EPiServer Content API
A generic content api for the EPiServer platform. The nuget comes with a standard set of "property converters" but this converters can be overridden and handled differently to suit your needs (make usage of CDN for example)

### Installation

    PM> Install-Package EOls.EPiContentApi

### Usage

    [GET] /api/{market}/content/{id}
    [GET] /api/content/{id}?locale={market}

    Example: /api/en/content/5

### EPiServer Page Example

The API will get all the properties that have either the <B>DisplayAttribute</B> or the <b>ApiPropertyAttribute</b> and cache the content.
```C#
public class StartPage
{
    [Display(GroupName = SystemTabNames.Content, Order = 10)]
    public virtual ContentArea MainContentArea { get; set; }

    [ApiProperty(Hide = true)]
    [Display(GroupName = Global.GroupNames.SiteSettings, Order = 20)]
    public virtual LinkItemCollection ProductPageLinks { get; set; } // This property will not be visible by the content API

    [ApiProperty]
    public DateTime CachedContent { get { return DateTime.Now; } } // This property will be cached

    [ApiProperty(Cache = false)]
    public DateTime NonCachedContent { get { return DateTime.Now; } } // This will not be cached

    [ApiProperty]
    public object SomeProperty
    {
        get
        {
            return new {
                FirstName = "Foo",
                LastName = "Bar"
            };
        }
    }
}
```

### Custom Property Converters
By using the IApiPropertyConverter interface you will be able to convert any property type to suit your needs.

```C#
public class CustomUrlConverter : IApiPropertyConverter<Url>
{
  public object Convert(Url obj, string locale)
  {
    // Here you are free to convert it any way you like
    return obj?.ToString();
  }
}
```

### Use Custom Converter
After creating your converter, you need to add it to the IPropertyConverterManager.
```C#
[InitializableModule]
[ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
public class ConfigureModule : IConfigurableModule
{
  public void Initialize(InitializationEngine context) {}

  public void Uninitialize(InitializationEngine context) {}

  public void ConfigureContainer(ServiceConfigurationContext context)
  {
  	context.ConfigurationComplete += Context_ConfigurationComplete;
  }

  private void Context_ConfigurationComplete(object sender, ServiceConfigurationEventArgs e)
  {
    ServiceLocator.Current.GetInstance<IPropertyConverterManager>
    ().ReplaceConverter(new CustomUrlConverter());
  }
}
```

### Caching
Due to heavy reflection operations, the content gets cached to improve performance. Pages and blocks gets cached individually and when you republish a page or a block that cache gets cleared.

This nuget currently uses a wrapper (<b>ICacheManager</b>) for EPiServers CacheManager which you can override in EPiServers ServiceLocator.

### Default output JSON
```C#
GlobalConfiguration.Configure(configuration =>
{
  configuration.Formatters.Clear();
  configuration.Formatters.Add(new JsonMediaTypeFormatter());
  configuration.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
});
```
