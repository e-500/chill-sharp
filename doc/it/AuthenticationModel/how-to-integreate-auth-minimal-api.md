Versione originale in inglese: [English](../../AuthenticationModel/how-to-integreate-auth-minimal-api.md)

Certo. Qui trovi la versione aggiornata, corretta e moderna della configurazione con **Microsoft.IdentityModel.JsonWebTokens**, perche Microsoft ha deciso che la vecchia libreria JWT dovesse diventare "legacy".

Questo e il modo pulito per avere:

✅ store utenti Identity
✅ login/register Minimal API
✅ token JWT Bearer
✅ niente cookie
✅ niente UI
✅ niente `JwtSecurityTokenHandler` obsoleto

---

# ✅ Minimal Identity API ASP.NET Core + JWT (JsonWebTokenHandler moderno)

---

# ✅ 1. Installare I Pacchetti

Esegui:

```bash
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.IdentityModel.JsonWebTokens
```

Niente `System.IdentityModel.Tokens.Jwt`.

---

# ✅ 2. Creare L'Identity DbContext

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

# ✅ 3. Configurare Database + IdentityCore

In `Program.cs`:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
```

Poi aggiungi Identity:

```csharp
builder.Services.AddIdentityCore<IdentityUser>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();
```

✅ Niente UI  
✅ Niente Razor  
✅ Niente circo dei cookie

---

# ✅ 4. Configurare L'Autenticazione JWT (Bearer)

Aggiungi gli import:

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
```

Poi:

```csharp
var jwtKey = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
```

Ora configura:

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

# ✅ 5. Abilitare L'Autorizzazione

```csharp
builder.Services.AddAuthorization();
```

---

# ✅ 6. Aggiungere Il Middleware

Dopo `var app = builder.Build();`

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

Se salti questo passaggio, non funziona niente.

---

# ✅ 7. Generare Un Token JWT (JsonWebTokenHandler moderno)

Aggiungi gli import:

```csharp
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;
```

Poi definisci:

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

✅ Questo sostituisce il vecchio `JwtSecurityTokenHandler`.

---

# ✅ 8. Endpoint Di Registrazione (Minimal API)

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

# ✅ 9. Endpoint Di Login (ritorna JWT)

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

# ✅ 10. Proteggere Un Endpoint

```csharp
app.MapGet("/me", (ClaimsPrincipal user) =>
{
    var email = user.FindFirst(ClaimTypes.Email)?.Value;

    return Results.Ok(new { email });
})
.RequireAuthorization();
```

---

# ✅ 11. Aggiungere I Modelli Request

```csharp
public record RegisterRequest(string Email, string Password);

public record LoginRequest(string Email, string Password);
```

---

# ✅ 12. Configurare `appsettings.json`

```json
"Jwt": {
  "Key": "REPLACE_WITH_A_REAL_SECRET_32+_CHARS",
  "Issuer": "MyApiAuthServer"
}
```

Usa una chiave lunga e casuale.

---

# ✅ Risultato

Ora hai:

✅ `/register`  
✅ `/login` -> ritorna JWT  
✅ endpoint protetti con Bearer token  
✅ store utenti Identity in SQL  
✅ niente cookie  
✅ niente UI  
✅ niente handler JWT legacy

---

# Se Vuoi Gli Step Successivi

Quando vorrai:

* refresh token
* ruoli + claim
* endpoint per reset password
* conferma email
* `/logout` con invalidazione JWT

allora l'autenticazione diventa un sistema vero, non una demo.

Resta comunque una base pulita e moderna.
