# EPiServer Content API
A generic content api for the EPiServer platform. The nuget comes with a standard set of "property converters" but this converters can be overridden and handled differently to suit your needs (make usage of CDN for example)
###Installation

    PM> Install-Package EOls.EPiContentApi

###Usage

    /api/{market}/content/{id}
    /api/content/{id}?locale={market}
    
    Example: /api/en/content/5

###EPiServer Page Example
The API will get all the properties that have either the <B>DisplayAttribute</B> or the <b>ApiPropertyAttribute</b>
```C#
class StartPage 
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

###Custom Property Converters
By using the IApiPropertyConverter interface you will be able to convert the property to suit your needs.

```C#
public class UrlConverter : IApiPropertyConverter<Url>
{
    public object Convert(Url obj, string locale)
    {
        // Here you are free to convert it any way you like
        return obj?.ToString();
    }
}
```

What's Next!

   * Attribute for hiding a whole class
   * Property converter method will be able to get its parent as a parameter
   * More documentation! 
