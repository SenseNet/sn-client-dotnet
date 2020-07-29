namespace SenseNet.Client.Authentication
{
    public class AuthorityInfo
    {
        public static AuthorityInfo Empty = new AuthorityInfo();

        public string Authority { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
    }
}
