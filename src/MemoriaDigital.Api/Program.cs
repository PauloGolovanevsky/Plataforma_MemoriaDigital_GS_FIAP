using MemoriaDigital.Application.Interfaces;
using MemoriaDigital.Application.Services;
using MemoriaDigital.Api.Middleware;
using MemoriaDigital.Infrastructure.Persistence;
using MemoriaDigital.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IAnalysisRepository, AnalysisRepository>();
builder.Services.AddScoped<IAnalysisService, AnalysisService>();
builder.Services.AddScoped<IAnalysisEngine, DeterministicAnalysisEngine>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}
