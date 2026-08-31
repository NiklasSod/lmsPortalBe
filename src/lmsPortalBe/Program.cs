using System.Text;
using lmsPortalBe.Data;
using lmsPortalBe.MappingProfiles;
using lmsPortalBe.Models;
using lmsPortalBe.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

var jwtSecret = builder.Configuration[JwtConstants.Secret]
    ?? throw new InvalidOperationException($"JWT configuration '{JwtConstants.Secret}' is missing.");
var jwtIssuer = builder.Configuration[JwtConstants.Issuer] ?? "lmsPortalBe";
var jwtAudience = builder.Configuration[JwtConstants.Audience] ?? "lmsPortalBe";

builder.Services.AddDbContext<LmsPortalContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddScoped<ILmsPortalContext>(sp =>
    sp.GetRequiredService<LmsPortalContext>());

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredUniqueChars = 0;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<LmsPortalContext>()
    .AddDefaultTokenProviders();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddAutoMapper(cfg => { }, typeof(AutoMapperProfile));

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddHostedService<RefreshTokenCleanupService>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<LmsPortalContext>();
    dbContext.Database.Migrate();
}

await app.SeedAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

/// <summary>
/// Entry point marker required by <c>WebApplicationFactory&lt;Program&gt;</c> in the
/// integration tests. The compiler-generated Program class is internal, so this
/// public partial declaration makes it accessible to the test project.
/// </summary>
public partial class Program { }
