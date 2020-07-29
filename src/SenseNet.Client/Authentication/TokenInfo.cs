using IdentityModel.Client;

namespace SenseNet.Client.Authentication
{
    public class TokenInfo
    {
        public string AccessToken { get; set; }
        public string IdentityToken { get; set; }
        public string TokenType { get; set; }
        public string RefreshToken { get; set; }
        public string ErrorDescription { get; set; }
        public int ExpiresIn { get; set; }
    }

    internal static class TokenProviderExtensions
    {
        public static TokenInfo ToTokenInfo(this TokenResponse response)
        {
            return new TokenInfo
            {
                AccessToken = response.AccessToken,
                ErrorDescription = response.ErrorDescription,
                ExpiresIn = response.ExpiresIn,
                IdentityToken = response.IdentityToken,
                RefreshToken = response.RefreshToken,
                TokenType = response.TokenType
            };
        }
    }
}
