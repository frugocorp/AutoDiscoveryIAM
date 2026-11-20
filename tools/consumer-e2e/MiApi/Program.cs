// Program.cs -- usings tal cual el README del paquete
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.Seeds;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Composition;
using Microsoft.EntityFrameworkCore;
using MiApi.Features;

var builder = WebApplication.CreateBuilder(args);

// De la plantilla dotnet new webapi
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AutoDiscoveryIAMDbContext>(options => options.UseSqlServer(
  connectionString,
  sql => sql
    .MigrationsAssembly("MiApi")
    .MigrationsHistoryTable("__EFMigrationsHistory_AutoDiscoveryIAM")));

builder.Services.AddAutoDiscoveryIAM(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseForwardedHeaders();
app.UseAutoDiscoveryIAM();
app.MapAutoDiscoveryIAMEndpoints();
app.MapMiApiEndpoints();

// De la plantilla: endpoint fuera de Features, debe aparecer en endpointsWithoutPermission
app.MapGet("/weatherforecast", () => new[] { "Chilly", "Mild" });

// Los scripts inician sesión con los usuarios de desarrollo: opt-in explícito (solo en Development).
await app.SeedAutoDiscoveryIAMDefaultDataAsync(includeDevelopmentUsers: true);

app.Run();
