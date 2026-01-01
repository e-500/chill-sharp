## Registering your DbContext with ChillSharp

1. Register your EF `DbContext` in DI as usual:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

2. Add ChillApi integration so the Chill controllers & services are registered and can share your `DbContext`:

```csharp
builder.Services.AddChillApi<AppDbContext>();
```

3. Map endpoints:

```csharp
app.MapChillApi();
```

This registers controllers (from ChillSharp assembly), wires `IChillContext` resolution and registers `IChillDtoEngine` that uses your context behind the scenes. ([GitHub][1])

---