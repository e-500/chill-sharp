Sure. Here’s the updated, correct, modern version of the setup using **Microsoft.IdentityModel.JsonWebTokens**, because Microsoft decided the old JWT library needed to be “legacy” for fun.

This is the clean way to do:

✅ Identity user store
✅ Minimal API login/register
✅ JWT Bearer tokens
✅ No cookies
✅ No UI
✅ No outdated JwtSecurityTokenHandler

---

# ✅ ASP.NET Core Minimal Identity API + JWT (Modern JsonWebTokenHandler)

---

# ✅ 1. Install Packages

Run:

```bash
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.IdentityModel.JsonWebTokens
```

No `System.IdentityModel.Tokens.Jwt`. We are enlightened now.

---

# ✅ 2. Create Identity DbContext

```csharp
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : IdentityDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }
}
```

---

# ✅ 3. Configure Database + IdentityCore

In `Program.cs`:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
```

Now add Identity:

```csharp
builder.Services.AddIdentityCore<IdentityUser>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();
```

✅ No UI
✅ No Razor
✅ No cookie circus

---

# ✅ 4. Configure JWT Authentication (Bearer)

Add imports:

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
```

Then:

```csharp
var jwtKey = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
```

Now configure:

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = false,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),

            ValidateLifetime = true
        };
    });
```

---

# ✅ 5. Enable Authorization

```csharp
builder.Services.AddAuthorization();
```

---

# ✅ 6. Add Middleware

After `var app = builder.Build();`

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

If you skip this, nothing works and you’ll blame yourself, incorrectly.

---

# ✅ 7. Generate JWT Token (Modern JsonWebTokenHandler)

Add imports:

```csharp
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;
```

Now define:

```csharp
string GenerateJwtToken(IdentityUser user)
{
    var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id),
        new Claim(JwtRegisteredClaimNames.Email, user.Email!)
    };

    var key = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(jwtKey));

    var creds = new SigningCredentials(
        key,
        SecurityAlgorithms.HmacSha256);

    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(claims),
        Expires = DateTime.UtcNow.AddHours(2),
        Issuer = jwtIssuer,
        SigningCredentials = creds
    };

    var handler = new JsonWebTokenHandler();

    return handler.CreateToken(tokenDescriptor);
}
```

✅ This is the replacement for the old JwtSecurityTokenHandler.

---

# ✅ 8. Register Endpoint (Minimal API)

```csharp
app.MapPost("/register", async (
    UserManager<IdentityUser> userManager,
    RegisterRequest request) =>
{
    var user = new IdentityUser
    {
        UserName = request.Email,
        Email = request.Email
    };

    var result = await userManager.CreateAsync(user, request.Password);

    if (!result.Succeeded)
        return Results.BadRequest(result.Errors);

    return Results.Ok("User registered");
});
```

---

# ✅ 9. Login Endpoint (Returns JWT)

```csharp
app.MapPost("/login", async (
    UserManager<IdentityUser> userManager,
    LoginRequest request) =>
{
    var user = await userManager.FindByEmailAsync(request.Email);

    if (user == null)
        return Results.Unauthorized();

    var valid = await userManager.CheckPasswordAsync(user, request.Password);

    if (!valid)
        return Results.Unauthorized();

    var token = GenerateJwtToken(user);

    return Results.Ok(new { token });
});
```

---

# ✅ 10. Protect an Endpoint

```csharp
app.MapGet("/me", (ClaimsPrincipal user) =>
{
    var email = user.FindFirst(ClaimTypes.Email)?.Value;

    return Results.Ok(new { email });
})
.RequireAuthorization();
```

---

# ✅ 11. Add Request Models

```csharp
public record RegisterRequest(string Email, string Password);

public record LoginRequest(string Email, string Password);
```

---

# ✅ 12. Configure appsettings.json

```json
"Jwt": {
  "Key": "REPLACE_WITH_A_REAL_SECRET_32+_CHARS",
  "Issuer": "MyApiAuthServer"
}
```

Make the key long and random, unless you want attackers logging in as you for sport.

---

# ✅ Result

Now you have:

✅ `/register`
✅ `/login` → returns JWT
✅ Protected endpoints with Bearer token
✅ Identity user store in SQL
✅ No cookies
✅ No UI
✅ No legacy JWT handler

---

# If You Want Next Steps

The moment you want:

* Refresh tokens
* Roles + claims
* Password reset endpoints
* Email confirmation
* `/logout` with JWT invalidation (annoying)

Then authentication becomes a *real* system, not a demo.

Still, this setup is the clean foundation Microsoft should’ve shipped by default.
