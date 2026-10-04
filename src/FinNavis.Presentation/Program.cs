using FinNavis.Application.DependencyInjection;
using FinNavis.Infrastructure.DependencyInjection;
using FinNavis.Presentation.Endpoints;
using FinNavis.Presentation.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapAccountEndpoints();

app.Run();

// Exposed so WebApplicationFactory<Program> can boot the API in integration tests.
public partial class Program;
