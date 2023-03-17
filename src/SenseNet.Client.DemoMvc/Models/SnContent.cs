namespace SenseNet.Client.DemoMvc.Models
{
    public class SnContent
    {
        public Content Content { get; set; }
        public IEnumerable<Content> Children { get; set; }
    }
}
