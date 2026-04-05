using Authentication.DL.Services;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace Authentication.API.Infrastructure
{
    public class RsaKeyService : IRsaKeyService
    {
        private readonly RSA _rsaPublic;

        public RsaSecurityKey PrivateKey { get; }
        public RsaSecurityKey PublicKey { get; }
        public string Kid { get; } = "ttl-rsa-key-1";
        public string Issuer { get; }

        public RsaKeyService(IConfiguration configuration, ILogger<RsaKeyService> logger)
        {
            Issuer = configuration["OpenId:Issuer"] ?? "http://localhost:5000";

            var privateKeyB64 = configuration["RsaKeys:PrivateKey"];
            var publicKeyB64 = configuration["RsaKeys:PublicKey"];

            var rsaPrivate = RSA.Create(2048);

            if (!string.IsNullOrWhiteSpace(privateKeyB64) && !string.IsNullOrWhiteSpace(publicKeyB64))
            {
                rsaPrivate.ImportRSAPrivateKey(Convert.FromBase64String(privateKeyB64), out _);
            }
            else
            {
                logger.LogWarning("RsaKeys not configured — generating ephemeral key pair. Tokens will be invalidated on restart.");
                logger.LogWarning("To persist keys, add to appsettings.json:");
                logger.LogWarning("  \"RsaKeys:PrivateKey\": \"{PrivateKey}\"", Convert.ToBase64String(rsaPrivate.ExportRSAPrivateKey()));
                logger.LogWarning("  \"RsaKeys:PublicKey\": \"{PublicKey}\"", Convert.ToBase64String(rsaPrivate.ExportRSAPublicKey()));
            }

            PrivateKey = new RsaSecurityKey(rsaPrivate) { KeyId = Kid };

            _rsaPublic = RSA.Create();
            _rsaPublic.ImportRSAPublicKey(rsaPrivate.ExportRSAPublicKey(), out _);
            PublicKey = new RsaSecurityKey(_rsaPublic) { KeyId = Kid };
        }

        public JsonWebKey GetPublicJwk()
        {
            var parameters = _rsaPublic.ExportParameters(false);
            return new JsonWebKey
            {
                Kty = JsonWebAlgorithmsKeyTypes.RSA,
                Use = "sig",
                Kid = Kid,
                Alg = SecurityAlgorithms.RsaSha256,
                N = Base64UrlEncoder.Encode(parameters.Modulus!),
                E = Base64UrlEncoder.Encode(parameters.Exponent!)
            };
        }
    }
}
