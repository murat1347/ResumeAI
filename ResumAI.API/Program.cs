using Scalar.AspNetCore;
using ResumeAI.Application;
using ResumeAI.Application.DTOs;
using ResumeAI.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// OpenAPI
builder.Services.AddOpenApi();

// LLM Settings from appsettings.json
builder.Services.Configure<LLMSettings>(builder.Configuration.GetSection("LLMSettings"));

// CORS for Angular frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add Application & Infrastructure services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors("AllowAngular");

app.UseAuthorization();

app.MapControllers();

app.Run();
