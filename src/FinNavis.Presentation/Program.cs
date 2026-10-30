using FinNavis.Application.DependencyInjection;
using FinNavis.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// The composition root, and the only place that names a layer's registrations. The connection
// string comes from configuration — never from a literal here.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// TODO: map feature endpoints (accounts, categories, transactions, transfers).

app.Run();

// Exposed so WebApplicationFactory<Program> can boot the API in integration tests.
public partial class Program;
