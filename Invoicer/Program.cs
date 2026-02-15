using Invoicer.Features;
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapEndpoints();

app.Run();
