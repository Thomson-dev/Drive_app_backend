using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using StackExchange.Redis;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
    ?? "localhost:6379";

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors.Select(error =>
                        string.IsNullOrWhiteSpace(error.ErrorMessage)
                            ? "Invalid value."
                            : error.ErrorMessage).ToArray());

            return new BadRequestObjectResult(new
            {
                message = "Please correct the request and try again.",
                errors,
                traceId = context.HttpContext.TraceIdentifier
            });
        };
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Drive App API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token to authorize requests"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
builder.Services.AddAuthentication(
    JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    builder.Configuration["Jwt:Key"]!))
        };
    });
builder.Services.AddSingleton<DbConnection>();
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConnectionString));
builder.Services.AddScoped<RideRepository>();
builder.Services.AddScoped<DriverOfferRepository>();
builder.Services.AddScoped<DriverProfileRepository>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<DriverOfferService>();
builder.Services.AddScoped<DriverLocationService>();
builder.Services.AddScoped<DriverProfileService>();
builder.Services.AddScoped<RideService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<JwtService>();

var app = builder.Build();

app.Use(async (context, next) =>
{
    try
    {
        await next(context);
    }
    catch (Exception ex)
    {
        if (context.Response.HasStarted)
        {
            throw;
        }

        var (status, message) = ex switch
        {
            ArgumentException => (StatusCodes.Status400BadRequest, ex.Message),
            PostgresException pg when pg.SqlState == PostgresErrorCodes.UniqueViolation
                && pg.ConstraintName == "users_email_key"
                => (StatusCodes.Status409Conflict, "An account with this email already exists."),
            PostgresException pg when pg.SqlState == PostgresErrorCodes.UniqueViolation
                => (StatusCodes.Status409Conflict, "This record already exists."),
            PostgresException pg when pg.SqlState == PostgresErrorCodes.UndefinedTable
                => (StatusCodes.Status503ServiceUnavailable,
                    "The service database is not initialized. Please contact support."),
            NpgsqlException => (StatusCodes.Status503ServiceUnavailable,
                "The database is temporarily unavailable. Please try again later."),
            _ => (StatusCodes.Status500InternalServerError,
                "Something went wrong. Please try again later.")
        };

        app.Logger.LogError(ex, "Request failed: {Method} {Path}",
            context.Request.Method, context.Request.Path);
        context.Response.Clear();
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new
        {
            message,
            traceId = context.TraceIdentifier
        });
    }
});

app.UseStatusCodePages(async statusContext =>
{
    var context = statusContext.HttpContext;
    var message = context.Response.StatusCode switch
    {
        StatusCodes.Status401Unauthorized => "Authentication is required.",
        StatusCodes.Status403Forbidden => "You do not have permission to do this.",
        StatusCodes.Status404NotFound => "The requested resource was not found.",
        StatusCodes.Status405MethodNotAllowed => "This method is not allowed for this endpoint.",
        _ => "The request could not be completed."
    };
    await context.Response.WriteAsJsonAsync(new
    {
        message,
        traceId = context.TraceIdentifier
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();
