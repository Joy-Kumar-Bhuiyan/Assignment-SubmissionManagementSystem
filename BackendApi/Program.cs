using System.Text;
using System.Threading.RateLimiting;
using BackendApi.Data;
using BackendApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

namespace BackendApi;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        builder.Host.UseSerilog();
        builder.Services.AddProblemDetails();

        if (builder.Environment.IsEnvironment("Testing"))
        {
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("InMemoryTestDb"));
        }
        else
        {
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:DefaultConnection is required. Configure it with environment variables or .NET user secrets.");
            }

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));
        }

        builder.Services.AddScoped<PasswordService>();
        builder.Services.AddScoped<AccessControlService>();
        builder.Services.AddScoped<AuditService>();
        builder.Services.AddScoped<NotificationService>();

        builder.Services.AddOptions<EmailOptions>()
            .Bind(builder.Configuration.GetSection(EmailOptions.SectionName))
            .Validate(options => !options.Enabled ||
                (!string.IsNullOrWhiteSpace(options.Host) &&
                 options.Port is >= 1 and <= 65535 &&
                 !string.IsNullOrWhiteSpace(options.Username) &&
                 !string.IsNullOrWhiteSpace(options.Password) &&
                 !string.IsNullOrWhiteSpace(options.FromAddress)),
                "Enabled email delivery requires a host, valid port, credentials, and from address.")
            .ValidateOnStart();
        builder.Services.AddTransient<LoggingEmailService>();
        builder.Services.AddTransient<SmtpEmailService>();
        builder.Services.AddScoped<IEmailService>(services =>
            services.GetRequiredService<Microsoft.Extensions.Options.IOptions<EmailOptions>>().Value.Enabled
                ? services.GetRequiredService<SmtpEmailService>()
                : services.GetRequiredService<LoggingEmailService>());

        var allowInsecureWebhook = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing");
        builder.Services.AddOptions<WebhookOptions>()
            .Bind(builder.Configuration.GetSection(WebhookOptions.SectionName))
            .Validate(options => !options.Enabled ||
                (Uri.TryCreate(options.Url, UriKind.Absolute, out var uri) &&
                 (uri.Scheme == Uri.UriSchemeHttps || (allowInsecureWebhook && uri.Scheme == Uri.UriSchemeHttp)) &&
                 options.TimeoutSeconds is >= 1 and <= 30),
                "Enabled webhooks require a valid HTTPS URL and a timeout from 1 to 30 seconds.")
            .ValidateOnStart();
        builder.Services.AddHttpClient<IOutgoingWebhookService, ConfiguredWebhookService>((services, client) =>
        {
            var options = services.GetRequiredService<Microsoft.Extensions.Options.IOptions<WebhookOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 1, 30));
        });

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddHealthChecks();

        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "School Management System API",
                Version = "v1",
                Description = "Assignment Management System API with JWT Bearer Token Support"
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
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
                    Array.Empty<string>()
                }
            });
        });

        var jwtKey = builder.Configuration["Jwt:Key"];
        if (builder.Environment.IsEnvironment("Testing") && string.IsNullOrWhiteSpace(jwtKey))
        {
            jwtKey = "TestingOnlyJwtSigningKey-AtLeast-32-Bytes-Long";
        }

        if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
        {
            throw new InvalidOperationException("Jwt:Key is required and must contain at least 32 bytes of unpredictable data.");
        }

        var jwtIssuer = builder.Configuration["Jwt:Issuer"];
        var jwtAudience = builder.Configuration["Jwt:Audience"];
        if (string.IsNullOrWhiteSpace(jwtIssuer) || string.IsNullOrWhiteSpace(jwtAudience))
        {
            throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience are required.");
        }

        var key = Encoding.UTF8.GetBytes(jwtKey);

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtAudience,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var userIdValue = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                        var tokenVersionValue = context.Principal?.FindFirst("token_version")?.Value;

                        if (!int.TryParse(userIdValue, out var userId) || !int.TryParse(tokenVersionValue, out var tokenVersion))
                        {
                            context.Fail("The access token is missing required claims.");
                            return;
                        }

                        var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
                        var email = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                        var role = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

                        if (user is null || user.TokenVersion != tokenVersion ||
                            !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase) ||
                            !string.Equals(user.Role, role, StringComparison.Ordinal))
                        {
                            context.Fail("The access token is no longer valid.");
                        }
                    }
                };
            });

        builder.Services.AddAuthorization();

        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (allowedOrigins.Length == 0)
        {
            throw new InvalidOperationException("Cors:AllowedOrigins must contain at least one trusted frontend origin.");
        }

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("Frontend", policy => policy
                .WithOrigins(allowedOrigins)
                .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                .WithHeaders("Authorization", "Content-Type"));
        });

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("login", httpContext => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
        });

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        });

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "School API v1");
                c.RoutePrefix = "swagger";
            });
        }
        else if (!app.Environment.IsEnvironment("Testing"))
        {
            app.UseHsts();
        }

        app.UseExceptionHandler();
        app.UseForwardedHeaders();
        if (!app.Environment.IsEnvironment("Testing"))
        {
            app.UseHttpsRedirection();
        }

        app.Use(async (context, next) =>
        {
            context.Response.Headers.XContentTypeOptions = "nosniff";
            context.Response.Headers.XFrameOptions = "DENY";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            await next();
        });

        app.UseSerilogRequestLogging();
        app.UseRateLimiter();
        app.UseCors("Frontend");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHealthChecks("/health").AllowAnonymous();

        if (!app.Environment.IsEnvironment("Testing"))
        {
            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (builder.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
            {
                await db.Database.MigrateAsync();
            }

            if (builder.Configuration.GetValue<bool>("SeedData:Enabled"))
            {
                var passwordService = scope.ServiceProvider.GetRequiredService<PasswordService>();
                await DbSeeder.SeedAsync(db, passwordService, builder.Configuration);
            }
        }

        await app.RunAsync();
    }
}
