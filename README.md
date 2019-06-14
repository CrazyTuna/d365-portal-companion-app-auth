# D365 Portal Companion APP
Sample API demonstrating how to configure OAuth to authenticate against D365 portal

### Official documentation

With the April 2019 release of D365 portal, there is a new way of authenticating to a `companion app`, check out the official documentation:

[Use OAuth 2.0 implicit grant flow within your portal](https://docs.microsoft.com/en-us/dynamics365/customer-engagement/portals/oauth-implicit-grant-flow)

### Configuring OAuth in your application

This sample API show how to configure OAuth on the companion app.    
It uses Asp net core 2.2 but the concept is the same for .Net framewrok applications.

You will have to specify only two settings in the `appsettings.json` file:

```json
  "D365Portal": {
    "Domain": "{app name}.microsoftcrmportals.com",
    "ApplicationId": "application id declared in D365 portal settings"
  },
```

The application is configured to validate the token as followed (see [`D365PortalAuthenticationExtensions.cs`](./D365CompanionApi/D365PortalAuthenticationExtensions.cs)):

```c#
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
```

It uses [`BouncyCastle.NetCore`](https://www.nuget.org/packages/BouncyCastle.NetCore) to get the `SecurityKey` from the public key exposes by D365 portal:

```c#
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
```
