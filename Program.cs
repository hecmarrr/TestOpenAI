using Microsoft.OpenApi.Models;
using SqlProcedureDeployer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SQL Script Upload API",
        Version = "v1",
        Description = "Servicio REST para cargar archivos SQL de procedimientos y permitir que otro aplicativo los consuma."
    });
});

builder.Services.AddSingleton<ISqlScriptStore, InMemorySqlScriptStore>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
