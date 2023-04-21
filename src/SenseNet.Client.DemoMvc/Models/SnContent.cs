namespace SenseNet.Client.DemoMvc.Models
{
    public class SnContent
    {
        public string CurrentUser { get; set; }
        public Content Content { get; set; }
        public IEnumerable<Content> Children { get; set; }
    }
}
