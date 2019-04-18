# D365 Portal Companion API
Sample API demonstrating how to configure OAuth to authenticate against D365 portal

### Official documentation

With the April 2019 release of D365 portal, there is a new way of authenticating to a `companion app`, check out the official documentation:

[Use OAuth 2.0 implicit grant flow within your portal](https://docs.microsoft.com/en-us/dynamics365/customer-engagement/portals/oauth-implicit-grant-flow)

### Configuring OAuth in your application

This sample API show how to configure OAuth on the companion app.    
It uses Asp net core 2.2 but the concept is the same for .Net framewrok applications.

You will have to specify only two settings in the `appsettings.json` file:

```
  "D365Portal": {
    "Domain": "{app name}.microsoftcrmportals.com",
    "ApplicationId": "application id declared in D365 portal settings"
  },
```

### TODO

- Get Feedback from Microsoft on the way of validating JTW token
- At the moment, the public key to verify the signed token if acquired when the app starts. If the certificate is renewed, we need to restart the app: Get the new public key automatically if the cert has been renewed.
