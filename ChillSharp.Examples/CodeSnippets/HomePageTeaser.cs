using ChillSharp;
using ChillSharp.Dto;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Assume you already have your DbContext (e.g., AppDbContext) configured
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add ChillSharp API using your existing DbContext
builder.Services.AddChillApi<AppDbContext>();

var app = builder.Build();

// Map ChillSharp API endpoints automatically
app.MapChillApi();

app.Run();