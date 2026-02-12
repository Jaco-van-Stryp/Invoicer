using System;
using System.Text;
using Amazon.SimpleEmailV2;
using Invoicer.Domain.Data;
using Invoicer.Features.Auth;
using Invoicer.Features.Company;
using Invoicer.Features.Products;
using Invoicer.Infrastructure.CurrentUserService;
using Invoicer.Infrastructure.EmailService;
using Invoicer.Infrastructure.EmailTemplateService;
using Invoicer.Infrastructure.ExceptionHandling;
using Invoicer.Infrastructure.JWTTokenService;
using Invoicer.Infrastructure.Validation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo { Title = "Invoicer API", Version = "v1" });

    // Add server URLs for API client generation
    opt.AddServer(
        new OpenApiServer { Url = "https://localhost:7261", Description = "Development HTTPS" }
    );
    opt.AddServer(
        new OpenApiServer { Url = "http://localhost:5244", Description = "Development HTTP" }
    );

    opt.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "Please enter token",
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            BearerFormat = "JWT",
            Scheme = "bearer",
        }
    );

    opt.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>(),
    });
});

// PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

var jwtOptions =
    builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration is missing");

builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization();

// CORS - Allow all origins
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        }
    );
});

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var sesOptions = builder.Configuration.GetSection("SES").Get<SesOptions>() ?? new SesOptions();
builder.Services.AddSingleton<IAmazonSimpleEmailServiceV2>(_ =>
{
    var region = Amazon.RegionEndpoint.GetBySystemName(sesOptions.Region);
    return new Amazon.SimpleEmailV2.AmazonSimpleEmailServiceV2Client(region);
});
builder.Services.Configure<SesOptions>(builder.Configuration.GetSection("SES"));
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddSingleton<IEmailTemplateService, EmailTemplateService>();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<IJwtTokenService, JtwTokenService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Map API Endpoints
app.MapAuthEndpoints();
app.MapCompanyEndpoints();
app.MapProductEndpoints();

app.Run();
