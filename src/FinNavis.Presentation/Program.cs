var builder = WebApplication.CreateBuilder(args);

// TODO: register Application services (builder.Services.AddApplication()).
// TODO: register Infrastructure services (builder.Services.AddInfrastructure(builder.Configuration)).
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
