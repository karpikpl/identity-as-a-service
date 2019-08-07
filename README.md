# identity-as-a-service workshop
This workshop follows examples from https://identityserver4.readthedocs.io/en/latest/index.html
It's a continuation of https://github.com/karpikpl/oidc-spa-client

## Build sample Identity As A Service project
```bash
dotnet new -i IdentityServer4.Templates

mkdir idaas && cd idaas
mkdir src && cd src

dotnet new is4empty -n IdentityServer
cd ..

dotnet new sln -n idaas
dotnet sln add .\src\IdentityServer\IdentityServer.csproj
```

## Protect the API
In *config.cs* add an API resource:
```csharp
public static IEnumerable<ApiResource> GetApis()
{
    return new ApiResource[] 
    {
        new ApiResource("api", "Acme Fireworks Co. payroll")
    };
}
```

add a client:
```csharp
return new Client[]
{
    new Client
    {
        ClientId = "client",
        // no interactive user, use the clientid/secret for authentication
        AllowedGrantTypes = GrantTypes.ClientCredentials,
        // secret for authentication
        ClientSecrets =
        {
            new Secret("secret".Sha256())
        },
        // scopes that client has access to
        AllowedScopes = { "api" }
    }
};
```

# Change Authority in the API project
```csharp
options.Authority = "http://localhost:5000";
options.RequireHttpsMetadata = false;
options.Audience = "api";
```

# Get the token
## Get the token
```bash
curl -v -X POST http://localhost:5000/connect/token -d "client_id=client&client_secret=secret&grant_type=client_credentials&scope=api" | json_pp
```

## Call the API
```bash
curl https://localhost:6001/api/secure -v -k --header "Authorization: Bearer xxx" | json_pp
```
where *xxx* is the **access_token**

## Inspect the token
http://jwt.io

## Add UI to IdentityServer
```bash
cd src/IdentityServer
dotnet new is4ui
```

In *startup.cs*:
```csharp
public void ConfigureServices(IServiceCollection services)
{
    // uncomment, if you want to add an MVC-based UI
    services.AddMvc().SetCompatibilityVersion(Microsoft.AspNetCore.Mvc.CompatibilityVersion.Version_2_1);
```
and
```csharp
// uncomment if you want to support static files
app.UseStaticFiles();

app.UseIdentityServer();

// uncomment, if you want to add an MVC-based UI
app.UseMvcWithDefaultRoute();
```

## Setup CORS
In *startup.cs* - *public void ConfigureServices(IServiceCollection services)*
```csharp
var cors = new DefaultCorsPolicyService(new LoggerFactory().CreateLogger<DefaultCorsPolicyService>())
{
    AllowAll = true
};
services.AddSingleton<ICorsPolicyService>(cors);
```

## Register UI Client
Add identity resources in *config.cs*:
```csharp
return new IdentityResource[]
{
    new IdentityResources.OpenId(),
    new IdentityResources.Profile(),
    new IdentityResources.Email()
};
```

## Add SPA client:
```csharp
new Client
  {
      ClientId = "spa",
      ClientName = "Single Page Javascript App",
      AllowedGrantTypes = GrantTypes.Code,
      // Specifies whether this client can request refresh tokens
      AllowOfflineAccess = true,
      RequireClientSecret = false,

      // where to redirect to after login
      RedirectUris = { "http://localhost:8080/callback.html" },

      // where to redirect to after logout
      PostLogoutRedirectUris = { "http://localhost:8080/logout.html" },

      AllowedScopes = new List<string>
      {
          IdentityServerConstants.StandardScopes.OpenId,
          IdentityServerConstants.StandardScopes.Profile,
          IdentityServerConstants.StandardScopes.Email,
          "api"
      }
```

## Add Users
In *startup.cs:*
```csharp
var builder = services.AddIdentityServer()
    .AddInMemoryIdentityResources(Config.GetIdentityResources())
    .AddInMemoryApiResources(Config.GetApis())
    .AddInMemoryClients(Config.GetClients())
    .AddTestUsers(Config.GetTestUsers());
```
In *config.cs*:
```csharp
internal static List<TestUser> GetTestUsers()
{
    return new List<TestUser>
    {
        new TestUser { SubjectId = "1", Username = "alice", Password = "alice",
            Claims =
            {
                new Claim(JwtClaimTypes.Name, "Alice Smith"),
                new Claim(JwtClaimTypes.Email, "AliceSmith@email.com")
            }
        },
        new TestUser { SubjectId = "11", Username = "bob", Password = "bob",
            Claims =
            {
                new Claim(JwtClaimTypes.Name, "Bob Smith"),
                new Claim(JwtClaimTypes.Email, "BobSmith@email.com")
            }
        }
    };
}
```

## Point SPA to local Identity Server
```js
 const config = {
    authority: 'http://localhost:5000/',
    client_id: 'spa',
    redirect_uri: 'http://localhost:8080/callback.html',
    response_type: 'code',
    scope: 'openid profile email api offline_access'
  };
```

## Run SPA and test
```bash
npx http-server
```
