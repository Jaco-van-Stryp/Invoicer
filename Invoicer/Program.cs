using Invoicer.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder
    .Services.AddSwaggerConfiguration(builder.Configuration, builder.Environment)
    .AddPostgres(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration)
    .AddAwsServices(builder.Configuration)
    .AddStorageServices(builder.Configuration)
    .AddApplicationServices(builder.Configuration, builder.Environment);

var app = builder.Build();

await app.ApplyMigrationsAsync();

app.ConfigureMiddleware();

app.Run();
