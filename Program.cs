using BE_API.Common;
using BE_API.Configuration;
using BE_API.Database;
using BE_API.Service;
using BE_API.Extensions;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using OfficeOpenXml;
using PayOS;
using Swashbuckle.AspNetCore.Filters;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

ExcelPackage.License.SetNonCommercialPersonal("SchoolBus");

var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigin", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.Configure<PayOsSettings>(builder.Configuration.GetSection("PayOS"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<FirebaseSettings>(builder.Configuration.GetSection("Firebase"));
builder.Services.Configure<FaceAISettings>(builder.Configuration.GetSection("FaceAI"));
builder.Services.Configure<CompreFaceSettings>(builder.Configuration.GetSection("CompreFace"));
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("Cloudinary"));
builder.Services.Configure<AppTimeSettings>(builder.Configuration.GetSection(AppTimeSettings.SectionName));
builder.Services.Configure<SeedAdminSettings>(builder.Configuration.GetSection(SeedAdminSettings.SectionName));
builder.Services.Configure<BookingSlotSettings>(builder.Configuration.GetSection(BookingSlotSettings.SectionName));
builder.Services.AddSingleton<IAppTime, AppTimeService>();
builder.Services.AddScoped<PayOSClient>(serviceProvider =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<PayOsSettings>>().Value;
    return new PayOSClient(settings.ClientId, settings.ApiKey, settings.ChecksumKey);
});

builder.Services.AddDbContext<BeContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            RoleClaimType = "Role"
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BE_API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập token theo định dạng: Bearer {token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new List<string>()
        }
    });

    c.EnableAnnotations();
    c.ExampleFilters();
});

builder.Services.AddSwaggerExamplesFromAssemblyOf<Program>();

builder.Services.Register();
builder.Services.AddHttpClient();

var app = builder.Build();

app.UseCors("AllowAllOrigin");

EnsureMigrate(app);
await app.EnsureSystemSeedDataAsync();
EnsureFirebaseInitialized(app);

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BE_API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static void EnsureMigrate(WebApplication webApp)
{
    using var scope = webApp.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<BeContext>();
    context.Database.Migrate();
}

static void EnsureFirebaseInitialized(WebApplication webApp)
{
    var logger = webApp.Logger;
    var settings = webApp.Configuration.GetSection("Firebase").Get<FirebaseSettings>() ?? new FirebaseSettings();

    if (!settings.Enabled)
    {
        logger.LogInformation("Firebase: disabled (Firebase:Enabled=false). Push notifications will not be sent.");
        return;
    }

    if (FirebaseApp.DefaultInstance != null)
    {
        logger.LogInformation("Firebase Admin SDK already initialized.");
        return;
    }

    if (string.IsNullOrWhiteSpace(settings.CredentialsPath))
    {
        logger.LogWarning("Firebase: Enabled but CredentialsPath is empty; Admin SDK will not initialize.");
        return;
    }

    var credentialPath = Path.IsPathRooted(settings.CredentialsPath)
        ? settings.CredentialsPath
        : Path.Combine(webApp.Environment.ContentRootPath, settings.CredentialsPath);

    if (!File.Exists(credentialPath))
    {
        logger.LogWarning("Firebase: credentials file not found at {CredentialPath}; Admin SDK will not initialize.", credentialPath);
        return;
    }

    var appOptions = new AppOptions
    {
        Credential = GoogleCredential.FromFile(credentialPath)
    };

    if (!string.IsNullOrWhiteSpace(settings.ProjectId))
        appOptions.ProjectId = settings.ProjectId;

    try
    {
        FirebaseApp.Create(appOptions);
        logger.LogInformation("Firebase Admin SDK initialized successfully (ProjectId={ProjectId}).",
            string.IsNullOrWhiteSpace(settings.ProjectId) ? "(default)" : settings.ProjectId);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Firebase: failed to create FirebaseApp. Push will be unavailable until configuration is fixed.");
    }
}
