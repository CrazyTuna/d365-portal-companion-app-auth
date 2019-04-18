using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace D365CompanionApi
{
    public static class D365PortalAuthenticationExtensions
    {
        public static AuthenticationBuilder AddD365PortalAuthentication(this IServiceCollection services, Action<D365PortalOptions> configureOptions)
        {
            D365PortalOptions d365PortalOptions = new D365PortalOptions();
            configureOptions(d365PortalOptions);

            return services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // Clock skew compensates for server time drift.
                    // We recommend 5 minutes or less:
                    ClockSkew = TimeSpan.FromMinutes(5),
                    // Specify the key used to sign the token:
                    RequireSignedTokens = true,
                    IssuerSigningKey = GetSecurityKey(d365PortalOptions).Result,
                    // Ensure the token hasn't expired:
                    RequireExpirationTime = true,
                    ValidateLifetime = true,
                    // Ensure the token audience matches our audience value (default true):
                    ValidateAudience = true,
                    ValidAudience = d365PortalOptions.ApplicationId,
                    // Ensure the token was issued by a trusted authorization server (default true):
                    ValidateIssuer = true,
                    ValidIssuer = d365PortalOptions.Domain
                };
            });
        }

        private static async Task<SecurityKey> GetSecurityKey(D365PortalOptions options)
        {
            string content = null;
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync($"https://{options.Domain}/_services/auth/publickey").ConfigureAwait(false);
                content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }

            var rs256Token = content.Replace("-----BEGIN PUBLIC KEY-----", "");
            rs256Token = rs256Token.Replace("-----END PUBLIC KEY-----", "");
            rs256Token = rs256Token.Replace("\n", "");
            var keyBytes = Convert.FromBase64String(rs256Token);

            var asymmetricKeyParameter = PublicKeyFactory.CreateKey(keyBytes);
            var rsaKeyParameters = (RsaKeyParameters)asymmetricKeyParameter;

            var rsa = new RSACryptoServiceProvider();
            var rsaParameters = new RSAParameters
            {
                Modulus = rsaKeyParameters.Modulus.ToByteArrayUnsigned(),
                Exponent = rsaKeyParameters.Exponent.ToByteArrayUnsigned()
            };
            rsa.ImportParameters(rsaParameters);

            return new RsaSecurityKey(rsa);
        }
    }
}
