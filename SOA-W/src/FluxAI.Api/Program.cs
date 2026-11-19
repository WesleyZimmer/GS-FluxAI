using System.Text.Json.Serialization;
using FluxAI.Api.Api;
using FluxAI.Api.Persistence;
using FluxAI.Api.Security;
using FluxAI.Api.Seeding;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
});

builder.Services
    .AddAuthentication(FluxAiAuthentication.Scheme)
    .AddScheme<AuthenticationSchemeOptions, FluxAiBearerHandler>(FluxAiAuthentication.Scheme, _ => { });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(FluxAiAuthentication.OperatorPolicy, policy => policy
        .AddAuthenticationSchemes(FluxAiAuthentication.Scheme)
        .RequireAuthenticatedUser()
        .RequireRole("operator"));
    options.AddPolicy(FluxAiAuthentication.WorkerPolicy, policy => policy
        .AddAuthenticationSchemes(FluxAiAuthentication.Scheme)
        .RequireAuthenticatedUser()
        .RequireRole("worker"));
    options.AddPolicy(FluxAiAuthentication.InternalReadPolicy, policy => policy
        .AddAuthenticationSchemes(FluxAiAuthentication.Scheme)
        .RequireAuthenticatedUser()
        .RequireRole("operator", "worker"));
});

var configuredConnection = builder.Configuration.GetConnectionString("FluxAi") ?? builder.Configuration.GetConnectionString("FluxIa") ?? "Data Source=App_Data/fluxia.db";
var sqliteConnection = new SqliteConnectionStringBuilder(configuredConnection);
if (!string.IsNullOrWhiteSpace(sqliteConnection.DataSource) && !Path.IsPathRooted(sqliteConnection.DataSource))
    sqliteConnection.DataSource = Path.GetFullPath(sqliteConnection.DataSource, builder.Environment.ContentRootPath);
var databaseDirectory = Path.GetDirectoryName(sqliteConnection.DataSource);
if (!string.IsNullOrWhiteSpace(databaseDirectory))
    Directory.CreateDirectory(databaseDirectory);

builder.Services.AddDbContext<FluxAiDbContext>(options => options.UseSqlite(sqliteConnection.ConnectionString));

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FluxAiDbContext>();
    await db.Database.MigrateAsync();
    await DemoDataSeeder.SeedAsync(db);
}

FluxAiEndpoints.Map(app);
RunEndpoints.Map(app);
await app.RunAsync();
