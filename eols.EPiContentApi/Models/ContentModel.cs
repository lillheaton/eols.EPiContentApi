namespace EOls.EPiContentApi.Models
{
    public class ContentModel
    {
        public int ContentId { get; set; }
        public string Url { get; set; }
        public string Name { get; set; }
        public int ContentTypeId { get; set; }
        public string PageTypeName { get; set; }
        public string LanguageBranch { get; set; }
        public object Content { get; set; }
    }
}