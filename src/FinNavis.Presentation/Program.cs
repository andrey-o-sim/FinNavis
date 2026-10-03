using FinNavis.Application.DependencyInjection;
using FinNavis.Infrastructure.DependencyInjection;
using FinNavis.Presentation.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Gives every error response an application/problem+json body. UseExceptionHandler() and
// UseStatusCodePages() below both depend on this being registered.
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// No argument: it only works because AddProblemDetails() is registered. Keep them together.
app.UseExceptionHandler();

// Turns a bare TypedResults.NotFound() into a problem+json body instead of an empty 404.
app.UseStatusCodePages();

app.UseHttpsRedirection();

app.MapAccountEndpoints();

app.Run();

// Exposed so WebApplicationFactory<Program> can boot the API in integration tests.
public partial class Program;
