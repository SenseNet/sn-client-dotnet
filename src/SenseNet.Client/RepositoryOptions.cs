using SenseNet.Client.Authentication;

namespace SenseNet.Client
{
    public class RepositoryOptions
    {
        public string Url { get; set; }
        public AuthenticationOptions Authentication { get; set; } = new AuthenticationOptions();
    }
}
