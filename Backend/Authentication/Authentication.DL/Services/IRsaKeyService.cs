using Microsoft.IdentityModel.Tokens;

namespace Authentication.DL.Services
{
    public interface IRsaKeyService
    {
        RsaSecurityKey PrivateKey { get; }
        RsaSecurityKey PublicKey { get; }
        string Kid { get; }
        string Issuer { get; }
        JsonWebKey GetPublicJwk();
    }
}
