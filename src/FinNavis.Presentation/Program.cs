using System.Text.Json;
using FinNavis.Application.DependencyInjection;
using FinNavis.Infrastructure.DependencyInjection;
using FinNavis.Presentation.Errors;

var builder = WebApplication.CreateBuilder(args);

// The composition root, and the only place that names a layer's registrations. The connection
// string comes from configuration — never from a literal here.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.AddErrorEnvelope();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);

var app = builder.Build();

// First in the pipeline, so a failure anywhere after it answers the same envelope.
app.UseErrorEnvelope();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// TODO: map feature endpoints (accounts, categories, transactions, transfers).

app.Run();

// Exposed so WebApplicationFactory<Program> can boot the API in integration tests.
public partial class Program;
