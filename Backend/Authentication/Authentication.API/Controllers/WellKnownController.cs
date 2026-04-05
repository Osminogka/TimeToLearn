using Authentication.DL.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;

namespace Authentication.API.Controllers
{
    [Route(".well-known")]
    [ApiController]
    public class WellKnownController : ControllerBase
    {
        private readonly IRsaKeyService _rsaKeyService;

        public WellKnownController(IRsaKeyService rsaKeyService)
        {
            _rsaKeyService = rsaKeyService;
        }

        [HttpGet("jwks.json")]
        public IActionResult GetJwks()
        {
            var jwk = _rsaKeyService.GetPublicJwk();
            var jwks = new JsonWebKeySet();
            jwks.Keys.Add(jwk);

            return Ok(new
            {
                keys = jwks.Keys.Select(k => new
                {
                    kty = k.Kty,
                    use = k.Use,
                    kid = k.Kid,
                    alg = k.Alg,
                    n = k.N,
                    e = k.E
                })
            });
        }

        [HttpGet("openid-configuration")]
        public IActionResult GetOpenIdConfiguration()
        {
            var issuer = _rsaKeyService.Issuer;

            return Ok(new
            {
                issuer,
                jwks_uri = $"{issuer}/.well-known/jwks.json",
                token_endpoint = $"{issuer}/api/a/authentication/login",
                id_token_signing_alg_values_supported = new[] { "RS256" },
                response_types_supported = new[] { "token" }
            });
        }
    }
}
