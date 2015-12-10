using System;

namespace EOls.EPiContentApi.Attributes
{
    public class ApiPropertyAttribute : Attribute
    {
        public bool Hide { get; set; }
    }
}